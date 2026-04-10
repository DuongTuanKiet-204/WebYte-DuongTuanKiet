using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebAppYte.Models;

namespace WebAppYte.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly modelWeb db = new modelWeb();

        [HttpPost]
        public async Task<JsonResult> Ask(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return Json(new
                    {
                        success = false,
                        answer = "Bạn vui lòng nhập câu hỏi để tôi hỗ trợ."
                    });
                }

                string question = message.Trim();

                if (question.Length < 2)
                {
                    return Json(new
                    {
                        success = false,
                        answer = "Câu hỏi quá ngắn. Bạn vui lòng nhập rõ hơn."
                    });
                }

                // 1. ưu tiên xử lý khẩn cấp
                string urgentAnswer = GetUrgentMedicalAnswer(question);
                if (!string.IsNullOrEmpty(urgentAnswer))
                {
                    SaveQuestionIfLoggedIn(question, urgentAnswer, 1, "Cảnh báo y tế khẩn từ AI");
                    return Json(new
                    {
                        success = true,
                        answer = urgentAnswer
                    });
                }

                // 2. ưu tiên FAQ local để tiết kiệm quota
                string faqAnswer = GetFaqAnswer(question);
                if (!string.IsNullOrEmpty(faqAnswer))
                {
                    return Json(new
                    {
                        success = true,
                        answer = faqAnswer
                    });
                }

                // 3. nếu là câu đơn giản thường gặp thì không gọi AI, tránh tốn lượt
                if (!ShouldUseGemini(question))
                {
                    string localFallback = "Bạn vui lòng mô tả rõ hơn triệu chứng hoặc vào mục HỎI ĐÁP ONLINE để bác sĩ/quản trị viên hỗ trợ chi tiết hơn. Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.";
                    SaveQuestionIfLoggedIn(question, localFallback, 0, "Fallback local do không cần gọi AI");
                    return Json(new
                    {
                        success = true,
                        answer = localFallback
                    });
                }

                // 4. gọi Gemini khi thật sự cần
                string aiAnswer = await AskGeminiAsync(question);

                if (string.IsNullOrWhiteSpace(aiAnswer))
                {
                    string fallback = "Tôi chưa thể trả lời chính xác lúc này. Bạn vui lòng gửi câu hỏi ở mục HỎI ĐÁP ONLINE để bác sĩ hoặc quản trị viên hỗ trợ thêm.";
                    SaveQuestionIfLoggedIn(question, fallback, 0, "Fallback do AI không phản hồi");
                    return Json(new
                    {
                        success = true,
                        answer = fallback
                    });
                }

                if (NeedEscalation(aiAnswer))
                {
                    SaveQuestionIfLoggedIn(question, aiAnswer, 0, "AI đề nghị chuyển sang hỏi đáp");
                }

                return Json(new
                {
                    success = true,
                    answer = aiAnswer
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    answer = "Chatbot đang tạm bận. Bạn vui lòng thử lại sau. Chi tiết: " + ex.Message
                });
            }
        }

        private async Task<string> AskGeminiAsync(string question)
        {
            string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
            string model = ConfigurationManager.AppSettings["GeminiModel"] ?? "gemini-2.5-flash";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "Chưa cấu hình Gemini API Key trong Web.config.";
            }

            string endpoint = $"https://generativelanguage.googleapis.com/v1/models/{model}:generateContent";

            var payload = new
            {
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = BuildMedicalPrompt(question) }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4,
                    topP = 0.9,
                    topK = 40,
                    maxOutputTokens = 1024
                }
            };

            string json = JsonConvert.SerializeObject(payload);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    var response = await client.PostAsync(endpoint, content);
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if ((int)response.StatusCode == 429)
                        {
                            return "AI đang tạm hết lượt miễn phí trong hôm nay. Bạn vui lòng dùng các câu hỏi gợi ý bên dưới hoặc vào mục HỎI ĐÁP ONLINE để được hỗ trợ thêm.";
                        }

                        return "Gemini lỗi: " + response.StatusCode + " | " + responseText;
                    }

                    var obj = JObject.Parse(responseText);

                    var parts = obj["candidates"]?
                        .FirstOrDefault()?["content"]?["parts"];

                    if (parts == null)
                    {
                        return null;
                    }

                    string fullText = string.Join(" ",
                        parts.Select(p => p["text"]?.ToString())
                             .Where(t => !string.IsNullOrEmpty(t)));

                    return fullText.Trim();
                }
            }
        }

        private string BuildMedicalPrompt(string question)
        {
            return
$@"Bạn là chatbot hỗ trợ y tế cơ bản cho website WebAppYte.

Nhiệm vụ:
- Trả lời bằng tiếng Việt, rõ ràng, dễ hiểu, lịch sự.
- Trả lời trọn câu, không được dừng giữa chừng.
- Chỉ cung cấp hướng dẫn y tế cơ bản và thông tin tham khảo.
- Không chẩn đoán chắc chắn bệnh.
- Nếu có dấu hiệu nguy hiểm như khó thở, đau ngực, ngất, co giật, chảy máu nhiều, sốt cao kéo dài thì phải khuyên người dùng đến cơ sở y tế hoặc cấp cứu ngay.
- Nếu câu hỏi ngoài phạm vi hoặc không chắc, hãy khuyên người dùng vào mục HỎI ĐÁP ONLINE.
- Không dùng thuật ngữ quá khó.
- Nếu người dùng chào hỏi, hãy trả lời thân thiện.
- Cuối câu trả lời thêm: 'Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.'

Câu hỏi người dùng: {question}";
        }

        private string GetUrgentMedicalAnswer(string question)
        {
            string q = question.ToLower();

            if (ContainsAny(q, "khó thở", "đau ngực", "ngất", "co giật", "chảy máu nhiều"))
            {
                return "Triệu chứng bạn mô tả có thể là dấu hiệu cần được kiểm tra sớm. Bạn nên đến cơ sở y tế gần nhất hoặc liên hệ cấp cứu ngay. Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.";
            }

            if (ContainsAny(q, "sốt cao liên tục", "sốt cao kéo dài"))
            {
                return "Nếu bạn sốt cao kéo dài hoặc kèm mệt nhiều, khó thở, co giật, bạn nên đi khám ngay để được đánh giá chính xác. Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.";
            }

            return null;
        }

        private string GetFaqAnswer(string question)
        {
            string q = question.ToLower();

            // chào hỏi
            if (ContainsAny(q, "hi", "hello", "xin chào", "chào", "chào bạn"))
                return "Xin chào, tôi là chatbot hỗ trợ y tế cơ bản của WebAppYte. Bạn có thể hỏi về triệu chứng thông thường, đặt lịch, trung tâm y tế, bác sĩ hoặc cách dùng website. Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.";

            // đau đầu / nhức đầu
            if (ContainsAny(q, "đau đầu", "nhức đầu"))
                return "Nếu bạn bị đau đầu hoặc nhức đầu nhẹ, bạn nên nghỉ ngơi, uống đủ nước, tránh thức khuya và theo dõi thêm. Nếu đau đầu kéo dài, đau nhiều, kèm sốt cao, nôn ói, mờ mắt hoặc yếu tay chân, bạn nên đi khám sớm. Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.";

            // sốt
            if (ContainsAny(q, "sốt", "bị sốt", "nóng sốt"))
                return "Nếu bạn bị sốt nhẹ, bạn nên nghỉ ngơi, uống nhiều nước, theo dõi nhiệt độ và giữ cơ thể thông thoáng. Nếu sốt cao kéo dài, khó thở, co giật hoặc mệt nhiều, bạn nên đến cơ sở y tế để được kiểm tra. Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.";

            // ho / đau họng
            if (ContainsAny(q, "ho", "đau họng", "viêm họng"))
                return "Nếu bạn bị ho hoặc đau họng nhẹ, bạn nên uống nước ấm, giữ ấm cơ thể, hạn chế đồ lạnh và theo dõi thêm. Nếu ho kéo dài, sốt cao, khó thở hoặc mệt nhiều, bạn nên đi khám bác sĩ. Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.";

            // đau bụng
            if (ContainsAny(q, "đau bụng"))
                return "Nếu bạn bị đau bụng nhẹ, bạn nên nghỉ ngơi, ăn thức ăn dễ tiêu và theo dõi thêm. Nếu đau bụng dữ dội, nôn nhiều, tiêu chảy kéo dài hoặc sốt cao, bạn nên đi khám sớm. Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.";

            // đăng ký
            if (ContainsAny(q, "đăng ký", "tạo tài khoản"))
                return "Bạn vào mục ĐĂNG KÝ trên thanh menu, điền đầy đủ thông tin rồi gửi biểu mẫu để tạo tài khoản.";

            // đăng nhập
            if (ContainsAny(q, "đăng nhập", "quên mật khẩu"))
                return "Bạn vào mục ĐĂNG NHẬP để truy cập hệ thống. Nếu quên mật khẩu, bạn nên liên hệ quản trị viên hoặc dùng chức năng khôi phục nếu website có hỗ trợ.";

            // lịch khám
            if (ContainsAny(q, "đặt lịch", "lịch khám", "hẹn khám"))
                return "Bạn có thể vào mục ĐẶT LỊCH HẸN để xem và tạo lịch khám hoặc lịch tư vấn video.";

            // bác sĩ
            if (ContainsAny(q, "bác sĩ", "danh sách bác sĩ"))
                return "Bạn vào mục DANH SÁCH BÁC SĨ để xem thông tin bác sĩ và lựa chọn phù hợp.";

            // trung tâm y tế
            if (ContainsAny(q, "trung tâm y tế", "bệnh viện", "cơ sở y tế", "gần nhất"))
                return "Bạn vào mục TRUNG TÂM Y TẾ để tra cứu các cơ sở y tế gần nhất.";

            // hỏi đáp
            if (ContainsAny(q, "hỏi đáp", "gửi câu hỏi", "tư vấn online"))
                return "Bạn có thể vào mục HỎI ĐÁP ONLINE để gửi câu hỏi cho bác sĩ hoặc quản trị viên.";

            // covid
            if (ContainsAny(q, "covid", "corona", "virus corona"))
                return "Nếu bạn nghi ngờ có triệu chứng liên quan đến COVID, hãy theo dõi sốt, ho, đau họng, khó thở và hạn chế tiếp xúc gần. Nếu triệu chứng nặng, bạn nên đi khám sớm. Thông tin chỉ mang tính tham khảo, không thay thế tư vấn bác sĩ.";

            return null;
        }

        private bool ShouldUseGemini(string question)
        {
            string q = question.ToLower();

            // câu quá ngắn hoặc quá phổ biến thì không cần gọi AI
            if (q.Length <= 15) return false;

            if (ContainsAny(q,
                "hi", "hello", "xin chào", "chào",
                "đau đầu", "nhức đầu",
                "sốt", "ho", "đau họng",
                "đau bụng",
                "đăng nhập", "đăng ký",
                "đặt lịch", "lịch khám",
                "bác sĩ",
                "trung tâm y tế",
                "covid",
                "hỏi đáp"))
            {
                return false;
            }

            return true;
        }

        private bool NeedEscalation(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return true;

            string a = answer.ToLower();
            return a.Contains("hỏi đáp online")
                || a.Contains("liên hệ bác sĩ")
                || a.Contains("liên hệ quản trị viên")
                || a.Contains("cơ sở y tế");
        }

        private void SaveQuestionIfLoggedIn(string question, string answer, int status, string note)
        {
            try
            {
                var user = Session["user"] as NguoiDung;
                if (user == null) return;

                var hoiDap = new HoiDap
                {
                    CauHoi = question,
                    TraLoi = answer,
                    IDNguoiDung = user.IDNguoiDung,
                    IDQuanTri = null,
                    NgayGui = DateTime.Now,
                    GhiChu = note,
                    TrangThai = status
                };

                db.HoiDaps.Add(hoiDap);
                db.SaveChanges();
            }
            catch
            {
            }
        }

        private bool ContainsAny(string input, params string[] keywords)
        {
            return keywords.Any(k => input.Contains(k));
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