using System;

namespace WebAppYte.Models
{
    public class NhatKySucKhoeViewModel
    {
        public int IDNhatKy { get; set; }

        public int IDNguoiDung { get; set; }

        public DateTime? NgayNhap { get; set; }

        public decimal? NhietDo { get; set; }

        public string TrieuChung { get; set; }

        public int? MucDoMet { get; set; }

        public string GhiChu { get; set; }

        public string HoTen { get; set; }
    }
}