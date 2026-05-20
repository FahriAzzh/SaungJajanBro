using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SAUNGJAJAN.Data;
using SAUNGJAJAN.Models;
using System.Security.Cryptography;
using System.Text;

namespace SAUNGJAJAN.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _db;

        public RegisterModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            // Fitur registrasi dinonaktifkan - gunakan aplikasi admin VB.NET
            TempData["ErrorMessage"] = "Registrasi dinonaktifkan. Hubungi admin untuk pembuatan akun.";
            return RedirectToPage("/Auth/Login");
        }

        public class InputModel
        {
            public string Nama { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string KonfirmasiPassword { get; set; } = string.Empty;
            public decimal Saldo { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Nama) || Input.Nama.Trim().Length < 3)
            {
                ErrorMessage = "Nama minimal 3 karakter.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.Email) || !Input.Email.Contains("@"))
            {
                ErrorMessage = "Format email tidak valid.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.Password) || Input.Password.Length < 6)
            {
                ErrorMessage = "Password minimal 6 karakter.";
                return Page();
            }

            if (Input.Password != Input.KonfirmasiPassword)
            {
                ErrorMessage = "Password dan konfirmasi tidak cocok.";
                return Page();
            }

            if (Input.Saldo < 0)
            {
                ErrorMessage = "Saldo tidak boleh negatif.";
                return Page();
            }

            var normalizedEmail = Input.Email.Trim().ToLower();

            var emailSudahAda = await _db.TbUser.AnyAsync(u => u.Email == normalizedEmail);
            if (emailSudahAda)
            {
                ErrorMessage = "Email sudah terdaftar. Gunakan email lain.";
                return Page();
            }

            var user = new TbUser
            {
                Nama = Input.Nama.Trim(),
                Email = normalizedEmail,
                Password = HashPassword(Input.Password),
                Saldo = Input.Saldo
            };

            _db.TbUser.Add(user);
            await _db.SaveChangesAsync();

            SuccessMessage = $"Akun '{Input.Nama}' berhasil dibuat. Silakan login.";
            Input = new InputModel();

            return Page();
        }

        private static string HashPassword(string password)
        {
            var salt = "SaungJajan2024!";
            var combined = salt + password;
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}