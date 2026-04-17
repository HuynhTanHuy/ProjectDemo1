-- BookPreviews table
CREATE TABLE [dbo].[BookPreviews]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [BookId] INT NOT NULL,
    [PreviewType] INT NOT NULL, -- 1 = PDF, 2 = Text
    [FilePath] NVARCHAR(500) NULL,
    [Content] NVARCHAR(MAX) NULL,
    [TotalPages] INT NOT NULL,
    [PreviewPages] INT NOT NULL,
    [AllowDownload] BIT NOT NULL CONSTRAINT [DF_BookPreviews_AllowDownload] DEFAULT(0),
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BookPreviews_CreatedAt] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [FK_BookPreviews_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_BookPreviews_PageLimit] CHECK ([PreviewPages] <= [TotalPages]),
    CONSTRAINT [CK_BookPreviews_TypePayload] CHECK (
        ([PreviewType] = 1 AND [FilePath] IS NOT NULL) OR
        ([PreviewType] = 2 AND [Content] IS NOT NULL)
    )
);
GO

CREATE INDEX [IX_BookPreviews_BookId] ON [dbo].[BookPreviews]([BookId]);
GO

-- UserPreviewLogs table (optional analytics)
CREATE TABLE [dbo].[UserPreviewLogs]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NULL,
    [BookId] INT NOT NULL,
    [ViewedAt] DATETIME2 NOT NULL CONSTRAINT [DF_UserPreviewLogs_ViewedAt] DEFAULT(SYSUTCDATETIME()),
    [DurationSeconds] INT NOT NULL CONSTRAINT [DF_UserPreviewLogs_DurationSeconds] DEFAULT(0),
    CONSTRAINT [FK_UserPreviewLogs_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserPreviewLogs_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_UserPreviewLogs_UserId] ON [dbo].[UserPreviewLogs]([UserId]);
GO

CREATE INDEX [IX_UserPreviewLogs_BookId] ON [dbo].[UserPreviewLogs]([BookId]);
GO
