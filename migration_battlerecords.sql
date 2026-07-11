BEGIN TRANSACTION;
GO

CREATE TABLE [BattleRecords] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [OpponentName] nvarchar(100) NOT NULL,
    [ProblemName] nvarchar(200) NOT NULL,
    [Language] nvarchar(50) NOT NULL,
    [Duration] nvarchar(50) NOT NULL,
    [Score] int NOT NULL,
    [IsWin] bit NOT NULL,
    [EloChange] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BattleRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BattleRecords_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_BattleRecords_UserId] ON [BattleRecords] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260711172655_AddBattleRecordsTable', N'8.0.0');
GO

COMMIT;
GO

