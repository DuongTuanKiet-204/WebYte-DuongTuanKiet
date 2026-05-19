using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebAppYte.Models;
using System.Data.Entity;

namespace WebAppYte.Controllers
{
    public class NhatKySucKhoeController : Controller
    {
        private modelWeb db = new modelWeb();

        // DANH SÁCH NHẬT KÝ CỦA BỆNH NHÂN
        public ActionResult Index(int? id)
        {
            var ds = db.Database.SqlQuery<NhatKySucKhoeViewModel>(@"
                SELECT *
                FROM NhatKySucKhoe
                WHERE IDNguoiDung = " + id + @"
                ORDER BY NgayNhap DESC").ToList();

            ViewBag.IDNguoiDung = id;

            return View(ds);
        }

        // FORM THÊM NHẬT KÝ
        public ActionResult Create(int? id)
        {
            ViewBag.IDNguoiDung = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection f)
        {
            int idNguoiDung = Convert.ToInt32(f["IDNguoiDung"]);

            decimal nhietDo = 0;
            decimal.TryParse(f["NhietDo"], out nhietDo);

            int mucDoMet = 0;
            int.TryParse(f["MucDoMet"], out mucDoMet);

            db.Database.ExecuteSqlCommand(@"
                INSERT INTO NhatKySucKhoe
                (
                    IDNguoiDung,
                    NgayNhap,
                    NhietDo,
                    TrieuChung,
                    MucDoMet,
                    GhiChu
                )
                VALUES
                (
                    @p0,
                    GETDATE(),
                    @p1,
                    @p2,
                    @p3,
                    @p4
                )",
                idNguoiDung,
                nhietDo,
                f["TrieuChung"],
                mucDoMet,
                f["GhiChu"]
            );

            return RedirectToAction("Index", new { id = idNguoiDung });
        }

        // BÁC SĨ XEM NHẬT KÝ
        public ActionResult BacsiXem(int? idNguoiDung)
        {
            var ds = db.Database.SqlQuery<NhatKySucKhoeViewModel>(@"
                SELECT *
                FROM NhatKySucKhoe
                WHERE IDNguoiDung = " + idNguoiDung + @"
                ORDER BY NgayNhap DESC").ToList();

            ViewBag.BenhNhan = db.NguoiDungs.Find(idNguoiDung);

            return View(ds);
        }
    }
}