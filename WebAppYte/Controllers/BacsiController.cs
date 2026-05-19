using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebAppYte.Models;
using PagedList;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Configuration;

namespace WebAppYte.Controllers
{
    public class BacsiController : Controller
    {
        private modelWeb db = new modelWeb();

        public ActionResult Index()
        {
            var quanTris = db.QuanTris.Include(q => q.Khoa).Where(x => x.VaiTro == 2);
            return View(quanTris.ToList());
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            QuanTri quanTri = db.QuanTris.Find(id);
            if (quanTri == null) return HttpNotFound();

            return View(quanTri);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            QuanTri quanTri = db.QuanTris.Find(id);
            if (quanTri == null) return HttpNotFound();

            ViewBag.IDKhoa = new SelectList(db.Khoas, "IDKhoa", "TenKhoa", quanTri.IDKhoa);
            return View(quanTri);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IDQuanTri,TaiKhoan,MatKhau,VaiTro,ThongTinBacSi,TrinhDo,IDKhoa,HoTen,AnhBia")] QuanTri quanTri)
        {
            if (ModelState.IsValid)
            {
                db.Entry(quanTri).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.IDKhoa = new SelectList(db.Khoas, "IDKhoa", "TenKhoa", quanTri.IDKhoa);
            return View(quanTri);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            QuanTri quanTri = db.QuanTris.Find(id);
            if (quanTri == null) return HttpNotFound();

            return View(quanTri);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            QuanTri quanTri = db.QuanTris.Find(id);
            db.QuanTris.Remove(quanTri);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult Quanlyhoidap(int? page)
        {
            var hoiDaps = db.HoiDaps
                .Include(h => h.NguoiDung)
                .Include(h => h.QuanTri)
                .Where(n => n.TrangThai == 0)
                .OrderByDescending(a => a.NgayGui)
                .ThenBy(a => a.IDHoidap)
                .ToList();

            int pageSize = 5;
            int pageNumber = page ?? 1;

            return View(hoiDaps.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Traloicauhoi(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            HoiDap hoiDap = db.HoiDaps.Find(id);
            if (hoiDap == null) return HttpNotFound();

            ViewBag.IDNguoiDung = new SelectList(db.NguoiDungs, "IDNguoiDung", "HoTen", hoiDap.IDNguoiDung);
            ViewBag.IDQuanTri = new SelectList(db.QuanTris, "IDQuanTri", "TaiKhoan", hoiDap.IDQuanTri);

            return View(hoiDap);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Traloicauhoi([Bind(Include = "IDHoidap,CauHoi,TraLoi,IDNguoiDung,IDQuanTri,NgayGui,GhiChu,TrangThai")] HoiDap hoiDap)
        {
            if (ModelState.IsValid)
            {
                hoiDap.TrangThai = 1;
                db.Entry(hoiDap).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Quanlyhoidap");
            }

            ViewBag.IDNguoiDung = new SelectList(db.NguoiDungs, "IDNguoiDung", "HoTen", hoiDap.IDNguoiDung);
            ViewBag.IDQuanTri = new SelectList(db.QuanTris, "IDQuanTri", "TaiKhoan", hoiDap.IDQuanTri);

            return View(hoiDap);
        }

        public ActionResult Kiemtralichhen(int? page)
        {
            var lich = db.LichKhams
                .Include(x => x.NguoiDung)
                .Include(x => x.QuanTri)
                .OrderByDescending(x => x.BatDau)
                .ThenBy(y => y.IDLichKham)
                .ToList();

            int pageSize = 10;
            int pageNumber = page ?? 1;

            return View(lich.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Lichdangcho(int? page)
        {
            var lich = db.LichKhams
                .Include(x => x.NguoiDung)
                .Include(x => x.QuanTri)
                .Where(x => x.TrangThai == 0 || x.TrangThai == 1)
                .OrderByDescending(x => x.BatDau)
                .ThenBy(y => y.IDLichKham)
                .ToList();

            int pageSize = 10;
            int pageNumber = page ?? 1;

            return View(lich.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Lichdaxacnhan(int? page)
        {
            var lich = db.LichKhams
                .Include(x => x.NguoiDung)
                .Include(x => x.QuanTri)
                .Where(x => x.TrangThai == 1)
                .OrderByDescending(x => x.BatDau)
                .ThenBy(y => y.IDLichKham)
                .ToList();

            int pageSize = 5;
            int pageNumber = page ?? 1;

            return View(lich.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Xacnhanlichhen(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            LichKham lichKham = db.LichKhams.Find(id);
            if (lichKham == null) return HttpNotFound();

            ViewBag.IDNguoiDung = new SelectList(
                db.NguoiDungs.Where(x => x.IDNguoiDung == lichKham.IDNguoiDung),
                "IDNguoiDung",
                "HoTen",
                lichKham.IDNguoiDung
            );

            ViewBag.IDQuanTri = new SelectList(
                db.QuanTris.Where(x => x.IDQuanTri == lichKham.IDQuanTri),
                "IDQuanTri",
                "HoTen",
                lichKham.IDQuanTri
            );

            return View(lichKham);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Xacnhanlichhen([Bind(Include = "IDLichKham,ChuDe,MoTa,BatDau,KetThuc,TrangThai,ZoomInfo,KetQuaKham,IDNguoiDung,IDQuanTri")] LichKham lichKham, string DonThuoc, string GhiChuBenhAn)
        {
            if (ModelState.IsValid)
            {
                db.Entry(lichKham).State = EntityState.Modified;
                db.SaveChanges();

                if (lichKham.TrangThai == 2)
                {
                    var bacSi = db.QuanTris.Find(lichKham.IDQuanTri);

                    var benhAn = db.BenhAns.FirstOrDefault(x => x.IDLichKham == lichKham.IDLichKham);

                    if (benhAn == null)
                    {
                        benhAn = new BenhAn();
                        benhAn.IDNguoiDung = lichKham.IDNguoiDung;
                        benhAn.IDLichKham = lichKham.IDLichKham;
                        benhAn.ThoiGian = DateTime.Now;
                        db.BenhAns.Add(benhAn);
                    }

                    benhAn.KetQua = lichKham.KetQuaKham;
                    benhAn.BacSyKham = bacSi != null ? bacSi.HoTen : "";
                    benhAn.DonThuoc = DonThuoc;

                    string tomTatAI = TaoTomTatAI(
                        lichKham.ChuDe,
                        lichKham.KetQuaKham,
                        GhiChuBenhAn
                    );

                    benhAn.GhiChu = tomTatAI;

                    db.SaveChanges();
                }

                return RedirectToAction("Kiemtralichhen", "Bacsi");
            }

            ViewBag.IDNguoiDung = new SelectList(db.NguoiDungs, "IDNguoiDung", "HoTen", lichKham.IDNguoiDung);
            ViewBag.IDQuanTri = new SelectList(db.QuanTris, "IDQuanTri", "TaiKhoan", lichKham.IDQuanTri);

            return View(lichKham);
        }

        private string TaoTomTatAI(string chuDe, string ketQua, string ghiChu)
        {
            try
            {
                string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];

                string prompt = $@"
Bạn là AI hỗ trợ y tế.

Hãy tóm tắt ngắn gọn bệnh án cho bác sĩ.

Nội dung tư vấn: {chuDe}

Kết quả khám: {ketQua}

Ghi chú thêm: {ghiChu}

Viết dạng:
- Triệu chứng
- Đã được tư vấn gì
- Cần theo dõi gì

Ngắn gọn, chuyên nghiệp.
";

                var requestData = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
                };

                string json = JsonConvert.SerializeObject(requestData);

                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    string url =
                        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key="
                        + apiKey;

                    var response = client.PostAsync(url, content).Result;

                    // Nếu Gemini bị nghẽn -> đợi 2 giây gọi lại
                    if ((int)response.StatusCode == 503)
                    {
                        System.Threading.Thread.Sleep(2000);

                        response = client.PostAsync(url, content).Result;
                    }

                    string result = response.Content.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic data = JsonConvert.DeserializeObject(result);

                        return data.candidates[0]
                                   .content.parts[0]
                                   .text.ToString();
                    }

                    // fallback nếu AI lỗi
                    return
                        "Bệnh nhân đã được bác sĩ tư vấn. " +
                        "Cần tiếp tục theo dõi sức khỏe tại nhà.";
                }
            }
            catch (Exception)
            {
                return
                    "Bệnh nhân đã được bác sĩ tư vấn. " +
                    "Cần tiếp tục theo dõi sức khỏe tại nhà.";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}