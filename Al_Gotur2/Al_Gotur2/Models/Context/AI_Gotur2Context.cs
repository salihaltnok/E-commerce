using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

namespace Al_Gotur2.Models.Context
{
    public class Al_Gotur2Context : DbContext
    {
        public Al_Gotur2Context(DbContextOptions<Al_Gotur2Context> options)
            : base(options)
        {
        }

        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Kategori> Kategoriler { get; set; }
        public DbSet<Urun> Urunler { get; set; }
        public DbSet<Siparis> Siparisler { get; set; }
        public DbSet<SiparisDetayi> SiparisDetaylari { get; set; }
        public DbSet<Sepet> Sepetler { get; set; }
        public DbSet<SepetUrunu> SepetUrunleri { get; set; }
        public DbSet<Favoriler> Favoriler { get; set; }
        public DbSet<UrunFiyatGuncellemeLog> UrunFiyatGuncellemeLog { get; set; }
        public DbSet<Yorum> Yorumlar { get; set; }
        public DbSet<Kupon> Kuponlar { get; set; }
        public DbSet<Adres> Adresler { get; set; }
        public DbSet<Kargo> Kargolar { get; set; }
        public DbSet<Indirim> Indirimler { get; set; }
        public DbSet<UrunDegisimLog> UrunDegisimLog { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Trigger sorununu çözmek için
            optionsBuilder.UseSqlServer(optionsBuilder.Options.Extensions.OfType<SqlServerOptionsExtension>().First().ConnectionString,
                options => options.EnableRetryOnFailure()
                                .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Primary Key tanımlamaları
            modelBuilder.Entity<Favoriler>()
                .HasKey(f => f.FavoriID);

            modelBuilder.Entity<SepetUrunu>()
                .HasKey(su => su.SepetUrunuID);

            modelBuilder.Entity<SiparisDetayi>()
                .HasKey(sd => sd.SiparisDetayID);
            modelBuilder.Entity<Sepet>()
                .HasKey(s => s.SepetID);

            // Kullanıcı ilişkileri
            modelBuilder.Entity<Kullanici>()
                .HasMany(k => k.Siparisler)
                .WithOne(s => s.Kullanici)
                .HasForeignKey(s => s.KullaniciID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Kullanici>()
                .HasMany(k => k.Favoriler)
                .WithOne(f => f.Kullanici)
                .HasForeignKey(f => f.KullaniciID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Kullanici>()
                .HasMany(k => k.Adresler)
                .WithOne(a => a.Kullanici)
                .HasForeignKey(a => a.KullaniciID)
                .OnDelete(DeleteBehavior.Cascade);

            // Kategori ilişkileri
            modelBuilder.Entity<Kategori>()
                .HasMany(k => k.Urunler)
                .WithOne(u => u.Kategori)
                .HasForeignKey(u => u.KategoriID)
                .OnDelete(DeleteBehavior.Restrict);

            // Ürün ilişkileri
            modelBuilder.Entity<Urun>()
                .HasMany(u => u.SiparisDetaylari)
                .WithOne(sd => sd.Urun)
                .HasForeignKey(sd => sd.UrunID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Urun>()
                .HasMany(u => u.Favoriler)
                .WithOne(f => f.Urun)
                .HasForeignKey(f => f.UrunID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Urun>()
                .HasMany(u => u.Yorumlar)
                .WithOne(y => y.Urun)
                .HasForeignKey(y => y.UrunID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Urun>()
                .HasMany(u => u.Indirimler)
                .WithOne(i => i.Urun)
                .HasForeignKey(i => i.UrunID)
                .OnDelete(DeleteBehavior.Cascade);

            

            modelBuilder.Entity<Siparis>(entity =>
            {
                entity.Property(e => e.ToplamTutar)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.OrijinalTutar)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.UygulananIndirim)
                    .HasColumnType("decimal(5,2)");
// Sipariş ilişkileri
            modelBuilder.Entity<Siparis>()
                .HasMany(s => s.SiparisDetaylari)
                .WithOne(sd => sd.Siparis)
                .HasForeignKey(sd => sd.SiparisID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Siparis>()
                .HasOne(s => s.Kargo)
                .WithOne(k => k.Siparis)
                .HasForeignKey<Kargo>(k => k.SiparisID)
                .OnDelete(DeleteBehavior.Restrict);
            });

            // Sepet ilişkileri
            modelBuilder.Entity<Sepet>()
                .HasMany(s => s.SepetUrunleri)
                .WithOne(su => su.Sepet)
                .HasForeignKey(su => su.SepetID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Sepet>()
                .HasOne(s => s.Kullanici)
                .WithOne()
                .HasForeignKey<Sepet>(s => s.KullaniciID)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Kullanici>()
                .Property(k => k.AdSoyad)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Kullanici>()
                .Property(k => k.Email)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Kullanici>()
                .HasIndex(k => k.Email)
                .IsUnique();

            modelBuilder.Entity<Kategori>()
                .Property(k => k.KategoriAdi)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Urun>()
                .Property(u => u.UrunAdi)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Urun>()
                .Property(u => u.Fiyat)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Siparis>()
                .Property(s => s.ToplamTutar)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SiparisDetayi>()
                .Property(sd => sd.Fiyat)
                .HasColumnType("decimal(18,2)");


            modelBuilder.Entity<SepetUrunu>()
                .Property(su => su.Fiyat)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Adres>()
                .Property(a => a.AdresBasligi)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Adres>()
                .Property(a => a.AdresDetay)
                .IsRequired()
                .HasMaxLength(250);

            modelBuilder.Entity<Adres>()
                .Property(a => a.Il) 
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Adres>()
                .Property(a => a.Ilce)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Adres>()
                .Property(a => a.PostaKodu)
                .HasMaxLength(10);

            modelBuilder.Entity<Kupon>()
                .Property(k => k.Kod)
                .IsRequired()
                .HasMaxLength(20);

            modelBuilder.Entity<Kupon>()
                .Property(k => k.IndirimOrani)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<Kargo>()
                .Property(k => k.KargoDurumu)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Kargo>()
                .Property(k => k.TakipNo)
                .HasMaxLength(50);

            modelBuilder.Entity<Kargo>()
                .Property(k => k.KargoFirmasi)
                .HasMaxLength(100);

            modelBuilder.Entity<Indirim>()
                .Property(i => i.IndirimOrani)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<Yorum>()
                .Property(y => y.YorumMetni)
                .HasMaxLength(500);

            // Varsayılan değerler
            modelBuilder.Entity<Favoriler>()
                .Property(f => f.EklenmeTarihi)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Siparis>()
                .Property(s => s.SiparisTarihi)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Yorum>()
                .Property(y => y.YorumTarihi)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Kupon>()
                .Property(k => k.Aktif)
                .HasDefaultValue(true);

            modelBuilder.Entity<Kargo>()
                .Property(k => k.KargoTarihi)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Indirim>()
                .Property(i => i.GecerlilikTarihi)
                .HasDefaultValueSql("DATEADD(month, 1, GETDATE())");

            modelBuilder.Entity<Indirim>()
                .Property(i => i.Aktif)
                .HasDefaultValue(true);

            // Soft Delete konfigürasyonu
            modelBuilder.Entity<Urun>()
                .Property<bool>("IsDeleted")
                .HasDefaultValue(false);

            modelBuilder.Entity<Siparis>()
                .Property<bool>("IsDeleted")
                .HasDefaultValue(false);
            modelBuilder.Entity<UrunFiyatGuncellemeLog>(entity =>
            {
                entity.HasKey(e => e.ID);

                entity.Property(e => e.UrunAdi)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.eskiFiyat)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.yeniFiyat)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.changedata)
                    .HasDefaultValueSql("GETDATE()");

                // Kategori ile ilişki
                entity.HasOne(e => e.Kategori)
                    .WithMany()
                    .HasForeignKey(e => e.KategoriID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UrunDegisimLog>(entity =>
            {
                entity.HasKey(e => e.ID);

                entity.Property(e => e.UrunAdi)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Fiyat)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.ChangeType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.changedata)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Kategori)
                    .WithMany()
                    .HasForeignKey(e => e.KategoriID)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Urun>().HasQueryFilter(u => !EF.Property<bool>(u, "IsDeleted"));
            modelBuilder.Entity<Siparis>().HasQueryFilter(s => !EF.Property<bool>(s, "IsDeleted"));
            modelBuilder.Entity<Indirim>().HasQueryFilter(i => i.Aktif);
        }
    }
}