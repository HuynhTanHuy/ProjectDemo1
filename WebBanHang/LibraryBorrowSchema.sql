CREATE TABLE [dbo].[Borrows]
(
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [BookId] INT NOT NULL,
    [BorrowDate] DATETIME2 NOT NULL,
    [DueDate] DATETIME2 NOT NULL,
    [ReturnDate] DATETIME2 NULL,
    [Status] INT NOT NULL, -- 1 = Borrowing, 2 = Returned
    CONSTRAINT [FK_Borrows_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Borrows_Products] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Borrows_UserId] ON [dbo].[Borrows]([UserId]);
GO
CREATE INDEX [IX_Borrows_BookId] ON [dbo].[Borrows]([BookId]);
GO

CREATE TABLE [dbo].[Penalties]
(
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [BorrowId] INT NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [Reason] NVARCHAR(500) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    CONSTRAINT [FK_Penalties_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Penalties_Borrows] FOREIGN KEY ([BorrowId]) REFERENCES [dbo].[Borrows]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Penalties_UserId] ON [dbo].[Penalties]([UserId]);
GO
CREATE INDEX [IX_Penalties_BorrowId] ON [dbo].[Penalties]([BorrowId]);
GO
