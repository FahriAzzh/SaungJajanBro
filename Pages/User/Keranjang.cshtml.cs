using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SAUNGJAJAN.Data;
using SAUNGJAJAN.Models;

namespace SAUNGJAJAN.Pages.User
{
    public class KeranjangModel : PageModel
    {
        private readonly AppDbContext _context;

        public KeranjangModel(AppDbContext context)
        {
            _context = context;
        }

        public string UserNama { get; set; } = string.Empty;

        public decimal UserSaldo { get; set; }

        public List<KeranjangTokoViewModel> KeranjangTokoList { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        private static string GenerateKodeKwitansi(int idPesanan)
        {
            return $"KW-{idPesanan:D6}";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            await LoadKeranjangAsync(userId.Value);

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateQuantityAsync(int idKeranjang, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            if (quantity <= 0)
            {
                ErrorMessage = "Jumlah produk harus lebih dari 0.";
                return RedirectToPage();
            }

            var keranjang = await _context.TbKeranjang
                .FirstOrDefaultAsync(k =>
                    k.IdKeranjang == idKeranjang &&
                    k.IdUser == userId.Value);

            if (keranjang == null)
            {
                ErrorMessage = "Data keranjang tidak ditemukan.";
                return RedirectToPage();
            }

            var produk = await _context.TbProduk
                .FirstOrDefaultAsync(p => p.IdProduk == keranjang.IdProduk);

            if (produk == null)
            {
                ErrorMessage = "Produk tidak ditemukan.";
                return RedirectToPage();
            }

            if (quantity > produk.Stok)
            {
                ErrorMessage = "Jumlah melebihi stok tersedia.";
                return RedirectToPage();
            }

            keranjang.Quantity = quantity;

            await _context.SaveChangesAsync();

            SuccessMessage = "Jumlah produk berhasil diperbarui.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int idKeranjang)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            var keranjang = await _context.TbKeranjang
                .FirstOrDefaultAsync(k =>
                    k.IdKeranjang == idKeranjang &&
                    k.IdUser == userId.Value);

            if (keranjang == null)
            {
                ErrorMessage = "Data keranjang tidak ditemukan.";
                return RedirectToPage();
            }

            _context.TbKeranjang.Remove(keranjang);

            await _context.SaveChangesAsync();
        
            SuccessMessage = "Produk berhasil dihapus dari keranjang.";
        
            return RedirectToPage();
        }
        
        public async Task<IActionResult> OnPostCheckoutSelectedAsync(
            List<int> selectedKeranjangIds,
            Dictionary<int, int> quantityUpdates)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
        
            if (userId == null)
            {
                return RedirectToPage("/Auth/Login");
            }
        
            if (selectedKeranjangIds == null || !selectedKeranjangIds.Any())
            {
                ErrorMessage = "Pilih minimal satu produk untuk dipesan.";
                return RedirectToPage();
            }
        
            await using var transaction = await _context.Database.BeginTransactionAsync();
        
            try
            {
                var user = await _context.TbUser
                    .FirstOrDefaultAsync(u => u.IdUser == userId.Value);
        
                if (user == null)
                {
                    ErrorMessage = "User tidak ditemukan.";
                    return RedirectToPage();
                }
        
                var keranjangList = await _context.TbKeranjang
                    .Where(k =>
                        k.IdUser == userId.Value &&
                        selectedKeranjangIds.Contains(k.IdKeranjang))
                    .ToListAsync();
        
                if (!keranjangList.Any())
                {
                    ErrorMessage = "Produk yang dipilih tidak ditemukan di keranjang.";
                    return RedirectToPage();
                }
        
                foreach (var item in keranjangList)
                {
                    if (quantityUpdates.ContainsKey(item.IdKeranjang))
                    {
                        item.Quantity = quantityUpdates[item.IdKeranjang];
                    }
        
                    if (item.Quantity <= 0)
                    {
                        ErrorMessage = "Jumlah produk harus lebih dari 0.";
                        return RedirectToPage();
                    }
                }
        
                var produkIds = keranjangList.Select(k => k.IdProduk).ToList();
        
                var produkList = await _context.TbProduk
                    .Where(p => produkIds.Contains(p.IdProduk))
                    .ToListAsync();
        
                foreach (var item in keranjangList)
                {
                    var produk = produkList.FirstOrDefault(p => p.IdProduk == item.IdProduk);
        
                    if (produk == null)
                    {
                        ErrorMessage = "Ada produk yang tidak ditemukan.";
                        return RedirectToPage();
                    }
        
                    if (item.Quantity > produk.Stok)
                    {
                        ErrorMessage = $"Stok {produk.NamaMakanan} tidak mencukupi.";
                        return RedirectToPage();
                    }
                }
        
                decimal totalSemua = keranjangList.Sum(item =>
                {
                    var produk = produkList.First(p => p.IdProduk == item.IdProduk);
                    return produk.Harga * item.Quantity;
                });
        
                if (user.Saldo < totalSemua)
                {
                    ErrorMessage = "Saldo tidak mencukupi untuk melakukan pemesanan.";
                    return RedirectToPage();
                }
        
                user.Saldo -= totalSemua;
        
                var groupByToko = keranjangList.GroupBy(k => k.IdToko).ToList();
        
                foreach (var group in groupByToko)
                {
                    int idToko = group.Key;
        
                    decimal totalToko = group.Sum(item =>
                    {
                        var produk = produkList.First(p => p.IdProduk == item.IdProduk);
                        return produk.Harga * item.Quantity;
                    });
        
                    var pesanan = new TbPesanan
                    {
                        IdUser = user.IdUser,
                        Status = "Diproses",
                        WaktuPesan = DateTime.Now,
                        TotalHarga = totalToko
                    };
        
                    _context.TbPesanan.Add(pesanan);
                    await _context.SaveChangesAsync();
        
                    foreach (var item in group)
                    {
                        var produk = produkList.First(p => p.IdProduk == item.IdProduk);
                        decimal subtotal = produk.Harga * item.Quantity;
        
                        var detail = new DetailPesanan
                        {
                            IdPesanan = pesanan.IdPesanan,
                            IdProduk = produk.IdProduk,
                            IdToko = idToko,
                            Quantity = item.Quantity,
                            Subtotal = subtotal
                        };
        
                        _context.DetailPesanan.Add(detail);
        
                        produk.Stok -= item.Quantity;
                    }
        
                    var pembayaran = new TbPembayaran
                    {
                        IdPesanan = pesanan.IdPesanan,
                        IdUser = user.IdUser,
                        IdToko = idToko,
                        KodeKwitansi = GenerateKodeKwitansi(pesanan.IdPesanan),
                        MetodePembayaran = "Saldo",
                        JumlahBayar = totalToko,
                        StatusPembayaran = "Ditahan",
                        WaktuDiteruskan = DateTime.Now
                    };
        
                    _context.TbPembayaran.Add(pembayaran);
                }
        
                _context.TbKeranjang.RemoveRange(keranjangList);
        
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
        
                HttpContext.Session.SetString(
                    "UserSaldo",
                    user.Saldo.ToString(System.Globalization.CultureInfo.InvariantCulture)
                );
        
                SuccessMessage = "Pesanan berhasil dibuat. Saldo sudah dipotong dan dana masih ditahan sampai toko menyiapkan pesanan.";

                // Dapatkan ID pesanan terakhir yang baru dibuat
                var lastPesananId = _context.TbPesanan
                    .Where(p => p.IdUser == userId.Value)
                    .OrderByDescending(p => p.IdPesanan)
                    .Select(p => p.IdPesanan)
                    .FirstOrDefault();

                // Logout user setelah checkout berhasil (sistem kios/antrian)
                HttpContext.Session.Remove("UserId");
                HttpContext.Session.Remove("UserNama");
                HttpContext.Session.Remove("UserSaldo");

                // Redirect ke halaman kwitansi untuk menunjukkan pesanan terakhir sebelum logout
                if (lastPesananId > 0)
                {
                    return RedirectToPage("/User/Kwitansi", new { IdPesanan = lastPesananId });
                }

                return RedirectToPage("/Auth/Login");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
        
                ErrorMessage = "Checkout gagal: " + ex.Message;
        
                return RedirectToPage();
            }
        }
        private async Task LoadKeranjangAsync(int userId)
        {
            var user = await _context.TbUser
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUser == userId);

            UserNama = user?.Nama ?? "-";
            UserSaldo = user?.Saldo ?? 0;

            var data = await (
                from keranjang in _context.TbKeranjang.AsNoTracking()
                join produk in _context.TbProduk.AsNoTracking()
                    on keranjang.IdProduk equals produk.IdProduk
                join toko in _context.TbToko.AsNoTracking()
                    on keranjang.IdToko equals toko.IdToko
                where keranjang.IdUser == userId
                orderby toko.NamaToko, produk.NamaMakanan
                select new
                {
                    keranjang.IdKeranjang,
                    keranjang.IdToko,
                    toko.NamaToko,
                    produk.IdProduk,
                    produk.NamaMakanan,
                    produk.Harga,
                    produk.Stok,
                    keranjang.Quantity
                }
            ).ToListAsync();

            KeranjangTokoList = data
                .GroupBy(x => new { x.IdToko, x.NamaToko })
                .Select(g => new KeranjangTokoViewModel
                {
                    IdToko = g.Key.IdToko,
                    NamaToko = g.Key.NamaToko,
                    TotalHarga = g.Sum(x => x.Harga * x.Quantity),
                    Detail = g.Select(x => new KeranjangItemViewModel
                    {
                        IdKeranjang = x.IdKeranjang,
                        IdProduk = x.IdProduk,
                        NamaProduk = x.NamaMakanan,
                        Harga = x.Harga,
                        Stok = x.Stok,
                        Quantity = x.Quantity,
                        Subtotal = x.Harga * x.Quantity
                    }).ToList()
                })
                .ToList();
        }

        public class KeranjangTokoViewModel
        {
            public int IdToko { get; set; }

            public string NamaToko { get; set; } = string.Empty;

            public decimal TotalHarga { get; set; }

            public List<KeranjangItemViewModel> Detail { get; set; } = new();
        }

        public class KeranjangItemViewModel
        {
            public int IdKeranjang { get; set; }

            public int IdProduk { get; set; }

            public string NamaProduk { get; set; } = string.Empty;

            public decimal Harga { get; set; }

            public int Stok { get; set; }

            public int Quantity { get; set; }

            public decimal Subtotal { get; set; }
        }
    }
}