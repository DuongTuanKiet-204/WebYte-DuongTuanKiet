using Google.Apis.Auth;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebAppYte.Models;

namespace WebAppYte.Controllers
{
    public class HomeController : Controller
    {
        private modelWeb db = new modelWeb();

        public ActionResult Index()
        {
            var solieu = db.Solieucovids.ToList();
            return View(solieu);
        }

        public ActionResult Trangchu()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Dangky()
        {
            ViewBag.IDGioiTinh = new SelectList(db.GioiTinhs, "IDGioiTinh", "GioiTinh1");
            ViewBag.IDTinh = new SelectList(db.TinhThanhs, "IDTinh", "TenTinh");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Dangky(NguoiDung nguoiDung)
        {
            ViewBag.IDGioiTinh = new SelectList(db.GioiTinhs, "IDGioiTinh", "GioiTinh1", nguoiDung.IDGioiTinh);
            ViewBag.IDTinh = new SelectList(db.TinhThanhs, "IDTinh", "TenTinh", nguoiDung.IDTinh);

            if (nguoiDung == null)
            {
                ViewBag.ErrorMessage = "Dữ liệu không hợp lệ.";
                return View();
            }

            nguoiDung.HoTen = nguoiDung.HoTen == null ? "" : nguoiDung.HoTen.Trim();
            nguoiDung.Email = nguoiDung.Email == null ? "" : nguoiDung.Email.Trim();
            nguoiDung.DienThoai = nguoiDung.DienThoai == null ? "" : nguoiDung.DienThoai.Trim();
            nguoiDung.TaiKhoan = nguoiDung.TaiKhoan == null ? "" : nguoiDung.TaiKhoan.Trim();
            nguoiDung.MatKhau = nguoiDung.MatKhau == null ? "" : nguoiDung.MatKhau.Trim();

            if (string.IsNullOrWhiteSpace(nguoiDung.HoTen) ||
                string.IsNullOrWhiteSpace(nguoiDung.Email) ||
                string.IsNullOrWhiteSpace(nguoiDung.DienThoai) ||
                string.IsNullOrWhiteSpace(nguoiDung.TaiKhoan) ||
                string.IsNullOrWhiteSpace(nguoiDung.MatKhau))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
                return View(nguoiDung);
            }

            if (nguoiDung.MatKhau.Length < 6)
            {
                ViewBag.ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
                return View(nguoiDung);
            }
            if (!nguoiDung.Email.Contains("@"))
            {
                ViewBag.ErrorMessage = "Email không hợp lệ.";
                return View(nguoiDung);
            }
            var taiKhoan = nguoiDung.TaiKhoan.Trim();
            var email = nguoiDung.Email.Trim();
            var dienThoai = nguoiDung.DienThoai.Trim();

            var checkTaiKhoan = db.NguoiDungs.FirstOrDefault(x => x.TaiKhoan.Trim() == taiKhoan);
            if (checkTaiKhoan != null)
            {
                ViewBag.ErrorMessage = "Tên tài khoản đã tồn tại.";
                return View(nguoiDung);
            }

            var checkEmail = db.NguoiDungs.FirstOrDefault(x => x.Email.Trim() == email);
            if (checkEmail != null)
            {
                ViewBag.ErrorMessage = "Email đã được sử dụng.";
                return View(nguoiDung);
            }

            var checkPhone = db.NguoiDungs.FirstOrDefault(x => x.DienThoai.Trim() == dienThoai);
            if (checkPhone != null)
            {
                ViewBag.ErrorMessage = "Số điện thoại đã được sử dụng.";
                return View(nguoiDung);
            }

            nguoiDung.TaiKhoan = taiKhoan;
            nguoiDung.Email = email;
            nguoiDung.DienThoai = dienThoai;

            try
            {
                db.NguoiDungs.Add(nguoiDung);
                db.SaveChanges();

                string subject = "Đăng ký tài khoản thành công";
                string body = "<div style='font-family:Arial,sans-serif;line-height:1.6'>" +
                              "<h2 style='color:#1d8cf8'>Đăng ký thành công</h2>" +
                              "<p>Xin chào <b>" + nguoiDung.HoTen + "</b>,</p>" +
                              "<p>Bạn đã đăng ký tài khoản thành công trên hệ thống <b>Trung Tâm Y Tế Quyền Lợi & Sức Khỏe</b>.</p>" +
                              "<p>Tên tài khoản của bạn là: <b>" + nguoiDung.TaiKhoan + "</b></p>" +
                              "<p>Chúc bạn có trải nghiệm tốt với hệ thống.</p>" +
                              "<br/><p>Trân trọng,<br/><b>Trung Tâm Y Tế Quyền Lợi & Sức Khỏe</b></p>" +
                              "</div>";

                try
                {
                    Common.SendMail.Send(nguoiDung.Email, subject, body);
                }
                catch
                {
                    // Không chặn luồng đăng ký nếu gửi mail lỗi
                }

                TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Dangnhap");
            }
            catch
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi đăng ký. Vui lòng thử lại.";
                return View(nguoiDung);
            }
        }

        [HttpGet]
        public ActionResult Dangnhap()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Dangnhap(FormCollection Dangnhap)
        {
            string tk = Dangnhap["TaiKhoan"] == null ? "" : Dangnhap["TaiKhoan"].ToString().Trim();
            string mk = Dangnhap["MatKhau"] == null ? "" : Dangnhap["MatKhau"].ToString().Trim();

            var islogin = db.NguoiDungs.SingleOrDefault(x => x.TaiKhoan.Equals(tk) && x.MatKhau.Equals(mk));
            var isloginAdmin = db.QuanTris.SingleOrDefault(x => x.TaiKhoan.Equals(tk) && x.MatKhau.Equals(mk));

            if (islogin != null)
            {
                Session["user"] = islogin;
                return RedirectToAction("Details", "Nguoidung", new { id = islogin.IDNguoiDung });
            }
            else if (isloginAdmin != null && isloginAdmin.VaiTro == 1)
            {
                Session["userAdmin"] = isloginAdmin;
                return RedirectToAction("QuanTris", "Admin");
            }
            else if (isloginAdmin != null && isloginAdmin.VaiTro == 2)
            {
                Session["userBS"] = isloginAdmin;
                return RedirectToAction("Trangchu", "Home");
            }
            else
            {
                ViewBag.Fail = "Tài khoản hoặc mật khẩu không chính xác.";
                return View("Dangnhap");
            }
        }

        public ActionResult DangXuat()
        {
            Session["user"] = null;
            return RedirectToAction("Index", "Home");
        }

        public ActionResult DangXuatBs()
        {
            Session["userBS"] = null;
            return RedirectToAction("Index", "Home");
        }

        public ActionResult TintucNewPartial()
        {
            string n = "new";
            var tintuc = db.Tintucs.Where(x => x.TheLoai.Equals(n));
            return PartialView(tintuc);
        }

        public ActionResult TintucHotPartial()
        {
            string h = "hot";
            var tintuc = db.Tintucs.Where(x => x.TheLoai.Equals(h));
            return PartialView(tintuc);
        }

        public ActionResult Xemchitiet(int IDTintuc = 0)
        {
            var chitiet = db.Tintucs.SingleOrDefault(n => n.IDTintuc == IDTintuc);
            if (chitiet == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            return View(chitiet);
        }

        [HttpGet]
        public ActionResult QuenMatKhau()
        {
            return View();
        }

        public string TaoOTP()
        {
            Random rd = new Random();
            return rd.Next(100000, 999999).ToString();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiOTP(string email)
        {
            email = email == null ? "" : email.Trim();

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập email.";
                return View("QuenMatKhau");
            }

            using (var dbContext = new modelWeb())
            {
                var user = dbContext.NguoiDungs.FirstOrDefault(x => x.Email == email);

                if (user == null)
                {
                    ViewBag.Error = "Email không tồn tại trong hệ thống!";
                    return View("QuenMatKhau");
                }

                string otp = TaoOTP();

                Session["OTP"] = otp;
                Session["EmailReset"] = email;
                Session["OTPExpireTime"] = DateTime.Now.AddMinutes(5);

                string subject = "Mã OTP khôi phục mật khẩu";
                string body = "<div style='font-family:Arial,sans-serif;line-height:1.6'>" +
                              "<h2 style='color:#1d8cf8'>Khôi phục mật khẩu</h2>" +
                              "<p>Mã OTP của bạn là:</p>" +
                              "<h1 style='color:#e74c3c;letter-spacing:4px;'>" + otp + "</h1>" +
                              "<p>Mã OTP này có hiệu lực trong vòng <b>5 phút</b>.</p>" +
                              "<p>Vui lòng không chia sẻ mã này cho người khác.</p>" +
                              "<br/><p>Trân trọng,<br/><b>Trung Tâm Y Tế Quyền Lợi & Sức Khỏe</b></p>" +
                              "</div>";

                try
                {
                    Common.SendMail.Send(email, subject, body);
                }
                catch
                {
                    ViewBag.Error = "Không gửi được email OTP. Vui lòng thử lại.";
                    return View("QuenMatKhau");
                }
            }

            return RedirectToAction("NhapOTP");
        }

        [HttpGet]
        public ActionResult NhapOTP()
        {
            if (Session["EmailReset"] == null)
            {
                return RedirectToAction("QuenMatKhau");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhanOTP(string otp)
        {
            otp = otp == null ? "" : otp.Trim();

            if (Session["OTP"] == null || Session["EmailReset"] == null || Session["OTPExpireTime"] == null)
            {
                ViewBag.Error = "Mã OTP đã hết hạn hoặc chưa được tạo.";
                return View("NhapOTP");
            }

            DateTime expireTime = Convert.ToDateTime(Session["OTPExpireTime"]);
            if (DateTime.Now > expireTime)
            {
                Session["OTP"] = null;
                Session["OTPExpireTime"] = null;
                ViewBag.Error = "Mã OTP đã hết hạn. Vui lòng gửi lại mã mới.";
                return View("NhapOTP");
            }

            if (otp == Session["OTP"].ToString())
            {
                return RedirectToAction("DatLaiMatKhau");
            }

            ViewBag.Error = "OTP không đúng. Vui lòng kiểm tra lại.";
            return View("NhapOTP");
        }

        [HttpGet]
        public ActionResult DatLaiMatKhau()
        {
            if (Session["EmailReset"] == null)
            {
                return RedirectToAction("QuenMatKhau");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatLaiMatKhau(string mk1, string mk2)
        {
            if (Session["EmailReset"] == null)
            {
                return RedirectToAction("QuenMatKhau");
            }

            mk1 = mk1 == null ? "" : mk1.Trim();
            mk2 = mk2 == null ? "" : mk2.Trim();

            if (string.IsNullOrWhiteSpace(mk1) || string.IsNullOrWhiteSpace(mk2))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            if (mk1.Length < 6)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return View();
            }

            if (mk1 != mk2)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            string email = Session["EmailReset"].ToString();

            using (var dbContext = new modelWeb())
            {
                var user = dbContext.NguoiDungs.FirstOrDefault(x => x.Email == email);

                if (user == null)
                {
                    ViewBag.Error = "Không tìm thấy người dùng.";
                    return View();
                }

                user.MatKhau = mk1;
                dbContext.SaveChanges();
            }

            Session["OTP"] = null;
            Session["EmailReset"] = null;
            Session["OTPExpireTime"] = null;

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction("Dangnhap");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        public class GoogleLoginRequest
        {
            public string Credential { get; set; }
        }

        public class GoogleLoginResponse
        {
            public bool success { get; set; }
            public string message { get; set; }
            public string redirectUrl { get; set; }
        }
        [HttpPost]
        public async Task<ActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Credential))
            {
                return Json(new GoogleLoginResponse
                {
                    success = false,
                    message = "Không nhận được thông tin đăng nhập Google."
                });
            }

            try
            {
                string clientId = ConfigurationManager.AppSettings["GoogleClientId"];

                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, settings);

                if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
                {
                    return Json(new GoogleLoginResponse
                    {
                        success = false,
                        message = "Không lấy được thông tin tài khoản Google."
                    });
                }

                string email = payload.Email.Trim();
                string hoTen = string.IsNullOrWhiteSpace(payload.Name) ? email.Split('@')[0] : payload.Name.Trim();

                var user = db.NguoiDungs.FirstOrDefault(x => x.Email.Trim() == email);

                if (user == null)
                {
                    string baseTaiKhoan = email.Split('@')[0];
                    string taiKhoanMoi = baseTaiKhoan;
                    int i = 1;

                    while (db.NguoiDungs.Any(x => x.TaiKhoan.Trim() == taiKhoanMoi))
                    {
                        taiKhoanMoi = baseTaiKhoan + i;
                        i++;
                    }

                    user = new NguoiDung
                    {
                        HoTen = hoTen,
                        Email = email,
                        TaiKhoan = taiKhoanMoi,
                        MatKhau = Guid.NewGuid().ToString("N").Substring(0, 12),
                        DienThoai = "",
                        IDGioiTinh = null,
                        IDTinh = null,
                        DiaChiCuThe = "",
                        SoCMND = null,
                        NhomMau = "",
                        ThongTinKhac = "Đăng nhập bằng Google"
                    };

                    db.NguoiDungs.Add(user);
                    db.SaveChanges();
                }

                Session["user"] = user;

                return Json(new GoogleLoginResponse
                {
                    success = true,
                    redirectUrl = Url.Action("Details", "Nguoidung", new { id = user.IDNguoiDung })
                });
            }
            catch
            {
                return Json(new GoogleLoginResponse
                {
                    success = false,
                    message = "Đăng nhập Google thất bại. Vui lòng thử lại."
                });
            }
        
        }

    }
}