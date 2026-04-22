using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace OpenVPN_GUI
{
    public partial class OpenVPN_Server : Form
    {
        // ================= CẤU HÌNH SUPABASE =================
        private const string SUPABASE_URL = "https://bbpbzayqytixodljcmsi.supabase.co";
        private const string SUPABASE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJicGJ6YXlxeXRpeG9kbGpjbXNpIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzY1OTQxNDYsImV4cCI6MjA5MjE3MDE0Nn0.9D_MQfrpZNfx7X7mBeZPV5TNBKrRW4oDRSsx3IcwriY";

        private const string OPENVPN_EXE_PATH = @"C:\Program Files\OpenVPN\bin\openvpn.exe";

        // ================= CẤU HÌNH PBKDF2 =================
        private const int Iterations = 100_000;
        private const int HashSize = 32;

        public OpenVPN_Server()
        {
            InitializeComponent();
            txtPassword.PasswordChar = '*';
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ==========================================================
            // TÍNH NĂNG ẨN DÀNH RIÊNG CHO BẠN ĐỂ LẤY MÃ HASH (DEV MODE)
            // ==========================================================
            if (username == "taomahash")
            {
                // Tự sinh mã chuẩn SHA-256 mới toanh
                string maMoiCung = GenerateHashToCopy(password);

                // Hiển thị hộp thoại để copy cho dễ, không lo dính dấu cách
                Form prompt = new Form() { Width = 500, Height = 120, Text = "Copy chuỗi dưới đây dán vào Supabase", StartPosition = FormStartPosition.CenterParent };
                TextBox textBox = new TextBox() { Left = 20, Top = 20, Width = 440, Text = maMoiCung, ReadOnly = true };
                prompt.Controls.Add(textBox);
                prompt.ShowDialog();

                return; // Dừng lại ở đây, không gọi API
            }
            // ==========================================================

            lblStatus.Text = "Đang xác thực...";
            lblStatus.ForeColor = System.Drawing.Color.Orange;
            btnLogin.Enabled = false;

            try
            {
                string ovpnContent = await CheckLoginAndGetConfig(username, password);
                if (ovpnContent == null)
                {
                    lblStatus.Text = "Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    lblStatus.Text = "Đăng nhập thành công! Đang khởi chạy OpenVPN...";
                    lblStatus.ForeColor = System.Drawing.Color.Blue;

                    string tempOvpnPath = Path.Combine(Path.GetTempPath(), $"{username}_config.ovpn");
                    File.WriteAllText(tempOvpnPath, ovpnContent);

                    StartOpenVPN(tempOvpnPath);

                    lblStatus.Text = "Đã gửi lệnh kết nối VPN thành công!";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            finally
            {
                btnLogin.Enabled = true;
            } 
        }

        // ================= HÀM GỌI API & KIỂM TRA MẬT KHẨU =================
        private async Task<string> CheckLoginAndGetConfig(string username, string rawPassword)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("apikey", SUPABASE_KEY);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + SUPABASE_KEY);

                // Chỉ tìm kiếm theo Username, lấy về cả Hash và File Config
                string apiUrl = $"{SUPABASE_URL}/rest/v1/vpn_users?username=eq.{username}&select=password_hash,ovpn_config";

                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JArray data = JArray.Parse(jsonResponse);

                    if (data.Count > 0)
                    {
                        string storedSaltHash = data[0]["password_hash"].ToString();
                        string ovpnConfig = data[0]["ovpn_config"].ToString();

                        // Xác thực mật khẩu cục bộ bằng PBKDF2
                        if (VerifyPassword(rawPassword, storedSaltHash))
                        {
                            return ovpnConfig; // Mật khẩu đúng, trả về file
                        }
                    }
                }
                return null; // Không tìm thấy user hoặc sai mật khẩu
            }
        }

        private bool VerifyPassword(string password, string storedSaltHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedSaltHash)) return false;
            var parts = storedSaltHash.Split(':');
            if (parts.Length != 2) return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] storedHash = Convert.FromBase64String(parts[1]);

                // ĐÃ TRẢ LẠI SHA256 VÀO ĐÂY ĐỂ ĐỒNG BỘ 100% VỚI LÒ LUYỆN
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] computedHash = pbkdf2.GetBytes(HashSize);
                    return SlowEquals(storedHash, computedHash);
                }
            }
            catch { return false; }
        }

        // Lò luyện đan Hash (ĐÃ THÊM LẠI SHA256)
        private string GenerateHashToCopy(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);
                return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
            }
        }

        // Hàm so sánh chống tấn công Timing Attack (Dùng cho .NET Framework cũ)
        private bool SlowEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            uint diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }

        private void StartOpenVPN(string configPath)
        {
            if (!File.Exists(OPENVPN_EXE_PATH))
            {
                MessageBox.Show("Không tìm thấy OpenVPN trên hệ thống.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = OPENVPN_EXE_PATH,
                Arguments = $"--config \"{configPath}\"",
                UseShellExecute = true, // Bắt buộc là true để xin quyền Admin
                Verb = "runas",         // Ép Windows hiện bảng hỏi quyền Admin (nếu App chưa có)
                WindowStyle = ProcessWindowStyle.Normal // Cho hiện cửa sổ log OpenVPN
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi khởi chạy OpenVPN: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
