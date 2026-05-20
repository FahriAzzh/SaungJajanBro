using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SAUNGJAJAN.Data;
using SAUNGJAJAN.Models;

namespace SAUNGJAJAN.Pages.User_Toko
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public TbToko? Toko { get; set; }

        public List<TbProduk> ProdukList { get; set; } = new();

        public List<PesananViewModel> PesananList { get; set; } = new();

        public int TotalProduk { get; set; }

        public int TotalStok { get; set; }

        public int TotalPesanan { get; set; }

        public decimal TotalPenjualanToko { get; set; }

        [BindProperty]
        public ProdukInputModel ProdukInput { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var idToko = GetCurrentTokoId();
        
            if (idToko == null)
            {
                return RedirectToPage("/Auth/LoginToko");
            }
        
            await LoadDashboardAsync(idToko.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostTambahProdukAsync()
        {
            var idToko = GetCurrentTokoId();
        
            if (idToko == null)
            {
                return RedirectToPage("/Auth/LoginToko");
            }
        
            try
            {
                if (string.IsNullOrWhiteSpace(ProdukInput.NamaMakanan))
                {
                    ErrorMessage = "Nama makanan wajib diisi.";
                    return RedirectToPage();
                }
        
                if (string.IsNullOrWhiteSpace(ProdukInput.Jenis))
                {
                    ErrorMessage = "Jenis produk wajib dipilih.";
                    return RedirectToPage();
                }
        
                if (ProdukInput.Harga <= 0)
                {
                    ErrorMessage = "Harga harus lebih dari 0.";
                    return RedirectToPage();
                }
        
                if (ProdukInput.Stok < 0)
                {
                    ErrorMessage = "Stok tidak boleh kurang dari 0.";
                    return RedirectToPage();
                }
        
                bool tokoAda = await _context.TbToko
                    .AnyAsync(t => t.IdToko == idToko.Value);
        
                if (!tokoAda)
                {
                    ErrorMessage = "Toko tidak ditemukan. Silakan login ulang sebagai toko.";
                    return RedirectToPage("/Auth/LoginToko");
                }
        
                var produk = new TbProduk
                {
                    IdToko = idToko.Value,
                    NamaMakanan = ProdukInput.NamaMakanan.Trim(),
                    Jenis = ProdukInput.Jenis.Trim(),
                    Harga = ProdukInput.Harga,
                    Stok = ProdukInput.Stok
                };
        
                _context.TbProduk.Add(produk);
                await _context.SaveChangesAsync();
        
                SuccessMessage = "Produk berhasil ditambahkan.";
                return RedirectToPage();
            }
            catch (DbUpdateException ex)
            {
                ErrorMessage = "Gagal menambahkan produk: " + ex.Message;
        
                if (ex.InnerException != null)
                {
                    ErrorMessage += " | Detail: " + ex.InnerException.Message;
                }
        
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Terjadi kesalahan: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int idPesanan, string status)
        {
            var idToko = GetCurrentTokoId();

            if (idToko == null)
            {
                return RedirectToPage("/Auth/LoginToko");
            }

            var daftarStatus = new[] { "Diproses", "Siap", "Dibatalkan" };

            if (string.IsNullOrWhiteSpace(status) || !daftarStatus.Contains(status))
            {
                ErrorMessage = "Status pesanan tidak valid.";
                return RedirectToPage();
            }

            bool pesananMilikToko = await _context.DetailPesanan
                .AnyAsync(d => d.IdPesanan == idPesanan && d.IdToko == idToko.Value);

            if (!pesananMilikToko)
            {
                ErrorMessage = "Pesanan tidak ditemukan untuk toko ini.";
                return RedirectToPage();
            }

            var pesanan = await _context.TbPesanan
                .FirstOrDefaultAsync(p => p.IdPesanan == idPesanan);

            if (pesanan == null)
            {
                ErrorMessage = "Pesanan tidak ditemukan.";
                return RedirectToPage();
            }

            string statusLama = pesanan.Status;
            string statusBaru = status.Trim();

            if (statusLama == statusBaru)
            {
                SuccessMessage = $"Tidak ada perubahan status pada pesanan #{pesanan.IdPesanan}.";
                return RedirectToPage();
            }

            if (statusLama.Equals("Siap", StringComparison.OrdinalIgnoreCase) &&
                statusBaru.Equals("Dibatalkan", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Pesanan yang sudah siap tidak bisa dibatalkan.";
                return RedirectToPage();
            }

            var pembayaran = await _context.TbPembayaran
                .FirstOrDefaultAsync(p => p.IdPesanan == idPesanan);

            if (pembayaran == null)
            {
                ErrorMessage = "Data pembayaran tidak ditemukan.";
                return RedirectToPage();
            }

            if (statusBaru.Equals("Siap", StringComparison.OrdinalIgnoreCase))
            {
                if (pembayaran.StatusPembayaran.Equals("Ditahan", StringComparison.OrdinalIgnoreCase))
                {
                    var toko = await _context.TbToko
                        .FirstOrDefaultAsync(t => t.IdToko == pembayaran.IdToko);

                    if (toko == null)
                    {
                        ErrorMessage = "Data toko tidak ditemukan.";
                        return RedirectToPage();
                    }

                    toko.Pemasukan += pembayaran.JumlahBayar;
                    pembayaran.StatusPembayaran = "Diteruskan";
                    pembayaran.WaktuDiteruskan = DateTime.Now;
                }
            }

            if (statusBaru.Equals("Dibatalkan", StringComparison.OrdinalIgnoreCase))
            {
                if (pembayaran.StatusPembayaran.Equals("Ditahan", StringComparison.OrdinalIgnoreCase))
                {
                    var user = await _context.TbUser
                        .FirstOrDefaultAsync(u => u.IdUser == pembayaran.IdUser);

                    if (user == null)
                    {
                        ErrorMessage = "Data user tidak ditemukan.";
                        return RedirectToPage();
                    }

                    user.Saldo += pembayaran.JumlahBayar;
                    pembayaran.StatusPembayaran = "Refund";

                    var detailList = await _context.DetailPesanan
                        .Where(d => d.IdPesanan == idPesanan)
                        .ToListAsync();

                    foreach (var detail in detailList)
                    {
                        var produk = await _context.TbProduk
                            .FirstOrDefaultAsync(p => p.IdProduk == detail.IdProduk);

                        if (produk != null)
                        {
                            produk.Stok += detail.Quantity;
                        }
                    }
                }
            }

            pesanan.Status = statusBaru;

            await _context.SaveChangesAsync();

            SuccessMessage = $"Status pesanan #{pesanan.IdPesanan} berhasil diperbarui dari \"{statusLama}\" menjadi \"{statusBaru}\".";

            return RedirectToPage();
        }
        private async Task LoadDashboardAsync(int idToko)
        {
            Toko = await _context.TbToko
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdToko == idToko);

            if (Toko == null)
            {
                ErrorMessage = "Data toko tidak ditemukan.";
                return;
            }

            var produkList = await _context.TbProduk
                .AsNoTracking()
                .Where(p => p.IdToko == idToko)
                .ToListAsync();

            var detailList = await (
                from detail in _context.DetailPesanan.AsNoTracking()
                join pesanan in _context.TbPesanan.AsNoTracking()
                    on detail.IdPesanan equals pesanan.IdPesanan
                join produk in _context.TbProduk.AsNoTracking()
                    on detail.IdProduk equals produk.IdProduk
                join user in _context.TbUser.AsNoTracking()
                    on pesanan.IdUser equals user.IdUser
                where detail.IdToko == idToko
                orderby pesanan.WaktuPesan descending
                select new
                {
                    detail.IdDetail,
                    detail.IdPesanan,
                    detail.IdProduk,
                    detail.Quantity,
                    detail.Subtotal,
                    pesanan.IdUser,
                    pesanan.Status,
                    pesanan.WaktuPesan,
                    pesanan.TotalHarga,
                    NamaPemesan = user.Nama,
                    NamaProduk = produk.NamaMakanan
                }
            ).ToListAsync();

            PesananList = detailList
                .GroupBy(d => d.IdPesanan)
                .Select(g =>
                {
                    var first = g.First();

                    return new PesananViewModel
                    {
                        IdPesanan = first.IdPesanan,
                        IdUser = first.IdUser,
                        NamaPemesan = first.NamaPemesan,
                        Status = first.Status,
                        WaktuPesan = first.WaktuPesan,
                        TotalHargaPesanan = first.TotalHarga,
                        TotalHargaToko = g.Sum(x => x.Subtotal),
                        TotalItem = g.Sum(x => x.Quantity),
                        Detail = g.Select(x => new DetailPesananViewModel
                        {
                            IdDetail = x.IdDetail,
                            IdProduk = x.IdProduk,
                            NamaProduk = x.NamaProduk,
                            Quantity = x.Quantity,
                            Subtotal = x.Subtotal
                        }).ToList()
                    };
                })
                .OrderByDescending(p => p.WaktuPesan)
                .ToList();

            TotalProduk = produkList.Count;
            TotalStok = produkList.Sum(p => p.Stok);
            TotalPesanan = PesananList.Count;

            TotalPenjualanToko = Toko.Pemasukan;
        }

        private int? GetCurrentTokoId()
        {
            int? sessionIdToko = HttpContext.Session.GetInt32("id_toko");

            if (sessionIdToko.HasValue)
            {
                return sessionIdToko.Value;
            }

            int? sessionTokoId = HttpContext.Session.GetInt32("TokoId");

            if (sessionTokoId.HasValue)
            {
                return sessionTokoId.Value;
            }

            return null;
        }

        public class ProdukInputModel
        {
            public string NamaMakanan { get; set; } = string.Empty;

            public string Jenis { get; set; } = "makanan";

            public decimal Harga { get; set; }

            public int Stok { get; set; }
        }

        public class PesananViewModel
        {
            public int IdPesanan { get; set; }

            public int IdUser { get; set; }

            public string NamaPemesan { get; set; } = string.Empty;

            public string Status { get; set; } = string.Empty;

            public DateTime WaktuPesan { get; set; }

            public decimal TotalHargaPesanan { get; set; }

            public decimal TotalHargaToko { get; set; }

            public int TotalItem { get; set; }

            public List<DetailPesananViewModel> Detail { get; set; } = new();
        }

        public class DetailPesananViewModel
        {
            public int IdDetail { get; set; }

            public int IdProduk { get; set; }

            public string NamaProduk { get; set; } = string.Empty;

            public int Quantity { get; set; }

            public decimal Subtotal { get; set; }
        }
    }
}