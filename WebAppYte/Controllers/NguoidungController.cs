using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using WebAppYte.Models;

namespace WebAppYte.Controllers
{
    public class NguoidungController : Controller
    {
        private modelWeb db = new modelWeb();

        public ActionResult Index()
        {
            var nguoiDungs = db.NguoiDungs
                .Include(n => n.GioiTinh)
                .Include(n => n.TinhThanh);

            return View(nguoiDungs.ToList());
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            NguoiDung nguoiDung = db.NguoiDungs.Find(id);

            if (nguoiDung == null)
            {
                return HttpNotFound();
            }

            ViewBag.id = nguoiDung.IDNguoiDung;

            ViewBag.BenhAn = db.BenhAns
                .Where(x => x.IDNguoiDung == nguoiDung.IDNguoiDung)
                .OrderByDescending(x => x.ThoiGian)
                .ToList();

            return View(nguoiDung);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            NguoiDung nguoiDung = db.NguoiDungs.Find(id);

            if (nguoiDung == null)
            {
                return HttpNotFound();
            }

            ViewBag.IDGioiTinh = new SelectList(db.GioiTinhs, "IDGioiTinh", "GioiTinh1", nguoiDung.IDGioiTinh);
            ViewBag.IDTinh = new SelectList(db.TinhThanhs, "IDTinh", "TenTinh", nguoiDung.IDTinh);

            return View(nguoiDung);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IDNguoiDung,HoTen,Email,DienThoai,TaiKhoan,MatKhau,IDGioiTinh,DiaChiCuThe,SoCMND,IDTinh,NhomMau,ThongTinKhac")] NguoiDung nguoiDung)
        {
            if (ModelState.IsValid)
            {
                db.Entry(nguoiDung).State = EntityState.Modified;
                db.SaveChanges();
                ViewBag.capnhat = " Cập nhật thành công ";
            }

            ViewBag.IDGioiTinh = new SelectList(db.GioiTinhs, "IDGioiTinh", "GioiTinh1", nguoiDung.IDGioiTinh);
            ViewBag.IDTinh = new SelectList(db.TinhThanhs, "IDTinh", "TenTinh", nguoiDung.IDTinh);

            return View(nguoiDung);
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