using Discord.WebSocket;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boomer
{
    public class DbMessage
    {
        public string Content { get; private set; }
        public DateTimeOffset CreateTime { get; private set; }
        public bool IsSuccessfulCommand { get; private set; }
        private DocumentReference MessageDoc { get; set; }
        private CollectionReference RecentMessagesColl { get; set; }

        private DbMessage() { }

        public static async Task<DbMessage> CreateAsync(SocketUserMessage msg, CollectionReference playerCollection, bool isSuccessfulCommand)
        {
            return await CreateAsync(msg.Content, msg.Timestamp, isSuccessfulCommand, msg.Author.Id, playerCollection,
                playerCollection.Document($"{msg.Author.Id}/recent-messages").Id);
        }

        public static async Task<DbMessage> CreateAsync(string content, DateTimeOffset createTime, bool isSuccessfulCommand, ulong authorId, CollectionReference playerCollection, string docId)
        {
            DbMessage dbMessage = new DbMessage();
            await dbMessage.InitializeAsync(content, createTime, isSuccessfulCommand, authorId, playerCollection, docId);
            return dbMessage;
        }

        private async Task InitializeAsync(string content, DateTimeOffset createTime, bool isSuccessfulCommand, ulong authorId, CollectionReference playerCollection, string docId)
        {
            Content = content;
            CreateTime = createTime;
            IsSuccessfulCommand = isSuccessfulCommand;

            RecentMessagesColl =  playerCollection.Document(authorId.ToString()).Collection("recent-messages");
            MessageDoc = RecentMessagesColl.Document(docId);

            if (!(await MessageDoc.GetSnapshotAsync()).Exists)
            {
                await MessageDoc.CreateAsync(new Dictionary<string, object>()
                {
                    { "content", Content },
                    { "create-time", CreateTime },
                    { "is-successful-command", isSuccessfulCommand }
                });
            }
        }

        public async Task DeleteAsync()
        {
            await MessageDoc.DeleteAsync();
        }
    }
}