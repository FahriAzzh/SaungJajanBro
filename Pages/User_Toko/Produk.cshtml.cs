using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SAUNGJAJAN.Data;
using SAUNGJAJAN.Models;

namespace SAUNGJAJAN.Pages.User_Toko
{
    public class ProdukModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProdukModel(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TbProduk> ProdukList { get; set; } = new List<TbProduk>();

        public string NamaToko { get; set; } = string.Empty;

        [BindProperty]
        public string? NamaMakanan { get; set; }

        [BindProperty]
        public string? Jenis { get; set; }

        [BindProperty]
        public decimal Harga { get; set; }

        [BindProperty]
        public int Stok { get; set; }

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

            // Get nama toko
            var toko = await _context.TbToko.FindAsync(idToko);
            if (toko != null)
            {
                NamaToko = toko.NamaToko;
            }

            ProdukList = await _context.TbProduk
                .Where(p => p.IdToko == idToko)
                .OrderByDescending(p => p.IdProduk)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProdukAsync()
        {
            var idToko = GetCurrentTokoId();
            if (idToko == null)
            {
                return RedirectToPage("/Auth/LoginToko");
            }

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Data produk tidak valid.";
                return await OnGetAsync();
            }

            var idProduk = Request.Form["idProduk"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(idProduk) || !int.TryParse(idProduk, out int productId))
            {
                // Tambah produk baru
                var produk = new TbProduk
                {
                    IdToko = idToko.Value,
                    NamaMakanan = NamaMakanan ?? string.Empty,
                    Jenis = Jenis ?? "makanan",
                    Harga = Harga,
                    Stok = Stok
                };

                _context.TbProduk.Add(produk);
            }
            else
            {
                // Update produk existing
                var produk = await _context.TbProduk.FindAsync(productId);
                if (produk == null || produk.IdToko != idToko)
                {
                    ErrorMessage = "Produk tidak ditemukan.";
                    return await OnGetAsync();
                }

                produk.NamaMakanan = NamaMakanan ?? string.Empty;
                produk.Jenis = Jenis ?? "makanan";
                produk.Harga = Harga;
                produk.Stok = Stok;

                _context.TbProduk.Update(produk);
            }

            await _context.SaveChangesAsync();
            SuccessMessage = "Produk berhasil disimpan.";
            return await OnGetAsync();
        }

        public async Task<IActionResult> OnPostHapusProdukAsync()
        {
            var idToko = GetCurrentTokoId();
            if (idToko == null)
            {
                return RedirectToPage("/Auth/LoginToko");
            }

            var idProduk = Request.Form["idProduk"].FirstOrDefault();
            if (string.IsNullOrEmpty(idProduk) || !int.TryParse(idProduk, out int productId))
            {
                ErrorMessage = "ID produk tidak valid.";
                return await OnGetAsync();
            }

            var produk = await _context.TbProduk.FindAsync(productId);
            if (produk == null || produk.IdToko != idToko)
            {
                ErrorMessage = "Produk tidak ditemukan.";
                return await OnGetAsync();
            }

            _context.TbProduk.Remove(produk);
            await _context.SaveChangesAsync();
            SuccessMessage = "Produk berhasil dihapus.";
            return await OnGetAsync();
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
    }
}
