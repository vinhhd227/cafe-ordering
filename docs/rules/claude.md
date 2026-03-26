# Tương tác với Claude

## Ngôn ngữ

- Trả lời bằng **tiếng Việt** trừ khi user hỏi bằng tiếng Anh
- Code, tên biến, tên file: luôn dùng tiếng Anh (theo chuẩn codebase)
- Comment trong code: tiếng Anh hoặc tiếng Việt tùy context

## Phong cách trả lời

- **Ngắn gọn, đi thẳng vào vấn đề** — không dẫn nhập dài dòng
- Không tóm tắt lại những gì vừa làm — user đọc được diff
- Không hỏi "Có muốn tôi tiếp tục không?" hay "Kết quả trông ổn không?" — cứ làm xong rồi báo cáo
- Chỉ giải thích khi logic không self-evident hoặc khi có trade-off quan trọng

## Dev Server / Long-running Process

- **Không tự khởi động** dev server, watch mode, hay bất kỳ process chạy dài
- **Hỏi trước** khi cần chạy: `pnpm dev`, `dotnet run`, `docker-compose up`, ...
- User quyết định khi nào start server

## Làm việc với Code

- **Đọc file trước** khi propose changes — không suggest sửa code chưa đọc
- Ưu tiên `Edit` tool (sửa file có sẵn) hơn `Write` (tạo mới)
- Không tạo file mới trừ khi thực sự cần thiết
- Không refactor code ngoài scope được yêu cầu
- Không thêm comments, type annotations, hay error handling cho code không được yêu cầu sửa

## Endpoint mới (.NET)

Khi tạo endpoint FastEndpoints mới, **luôn tạo kèm Summary class** trong cùng folder:
- Tên: `{EndpointName}Summary`
- Kế thừa: `Summary<{EndpointName}Endpoint>`
- Bắt buộc có: Summary, Description, ResponseExamples cho 200, tất cả Response codes

## Git / Commit

- **Không tự commit** trừ khi được yêu cầu rõ ràng
- Không amend commit đã có — luôn tạo commit mới
- Không force push
- Không bypass hooks (`--no-verify`)

## Khi gặp vấn đề

- Không brute-force retry cùng một action khi bị block
- Tìm nguyên nhân gốc rễ, không workaround qua symptoms
- Nếu bị block và không biết hướng giải quyết → hỏi user
