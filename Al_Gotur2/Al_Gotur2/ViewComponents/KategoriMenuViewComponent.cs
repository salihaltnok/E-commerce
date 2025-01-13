using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Al_Gotur2.Models;
using Al_Gotur2.Models.Context;

namespace Al_Gotur2.ViewComponents
{
    public class KategoriMenuViewComponent : ViewComponent
    {
        private readonly Al_Gotur2Context _context;

        public KategoriMenuViewComponent(Al_Gotur2Context context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var kategoriler = await _context.Kategoriler.ToListAsync();
            return View(kategoriler);
        }
    }
}