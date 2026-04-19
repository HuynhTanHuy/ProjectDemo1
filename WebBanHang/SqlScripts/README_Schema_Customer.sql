/*
  WebBanHang — quan hệ CSDL (SQL Server) hiện có qua Entity Framework
  
  Bảng chính:
  - AspNetUsers (+ Identity) — người dùng
  - Products — sách/sản phẩm (FK: CategoryId bắt buộc, AuthorId, GenreId, PublisherId tùy chọn)
  - Authors, Genres, Categories, Publishers — thực thể tra cứu
  - Reviews — ProductId, UserId
  - Orders / OrderDetails — giao dịch
  - Borrows — UserId, BookId (Product)

  EF Core đã khai báo quan hệ Product → Author, Category, Genre, Publisher trong migration
  (xem Migrations/ApplicationDbContextModelSnapshot.cs, khối WebBanHang.Models.Product).

  Nếu CSDL cũ thiếu FK ở SQL Server, có thể thêm thủ công (kiểm tra tên constraint trước khi chạy):

  ALTER TABLE Products WITH CHECK
    ADD CONSTRAINT FK_Products_Authors_AuthorId
    FOREIGN KEY (AuthorId) REFERENCES Authors(Id);

  ALTER TABLE Products WITH CHECK
    ADD CONSTRAINT FK_Products_Genres_GenreId
    FOREIGN KEY (GenreId) REFERENCES Genres(Id);

  ALTER TABLE Products WITH CHECK
    ADD CONSTRAINT FK_Products_Publishers_PublisherId
    FOREIGN KEY (PublisherId) REFERENCES Publishers(Id);

  (CategoryId thường đã có FK CASCADE từ migration ban đầu.)

  Many-to-many BookAuthors: hiện tại ứng dụng dùng AuthorId đơn trên Product; nếu cần nhiều tác giả/sách,
  thêm bảng BookAuthors(BookId, AuthorId) và migration EF riêng.
*/
