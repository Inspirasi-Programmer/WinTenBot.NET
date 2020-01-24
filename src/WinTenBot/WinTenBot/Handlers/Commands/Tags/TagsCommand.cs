﻿using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.LiteDB;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using WinTenBot.Helpers;
using WinTenBot.Helpers.Processors;
using WinTenBot.Services;

namespace WinTenBot.Handlers.Commands.Tags
{
    public class TagsCommand : CommandBase
    {
        private readonly TagsService _tagsService;
        private SettingsService _settingsService;
        private ChatProcessor _chatProcessor;

        public TagsCommand()
        {
            _tagsService = new TagsService();
        }

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args,
            CancellationToken cancellationToken)
        {
            Message msg = context.Update.Message;
            _chatProcessor = new ChatProcessor(context);
            _settingsService = new SettingsService(msg);

            var id = msg.From.Id;
            var sendText = "Under maintenance";

            ConsoleHelper.WriteLine(id.IsSudoer());
            
            await _chatProcessor.DeleteAsync(msg.MessageId);
            await _chatProcessor.SendAsync("🔄 Loading tags..");
            var tagsData = await _tagsService.GetTagsByGroupAsync("*", msg.Chat.Id);
            var tagsStr = string.Empty;

            foreach (var tag in tagsData)
            {
                tagsStr += $"#{tag.Tag} ";
            }

            sendText = $"#️⃣<b> {tagsData.Count} Tags</b>\n" +
                       $"\n{tagsStr}";

            await _chatProcessor.EditAsync(sendText);
            
            //            var jsonSettings = TextHelper.ToJson(currentSetting);
            //            ConsoleHelper.WriteLine($"CurrentSettings: {jsonSettings}");

            // var lastTagsMsgId = int.Parse(currentSetting.Rows[0]["last_tags_message_id"].ToString());

            var currentSetting = await _settingsService.GetSettingByGroup();
            var lastTagsMsgId = currentSetting.LastTagsMessageId;
            ConsoleHelper.WriteLine($"LastTagsMsgId: {lastTagsMsgId}");

            await _chatProcessor.DeleteAsync(lastTagsMsgId.ToInt());

            await _tagsService.UpdateCacheAsync(msg);

            ConsoleHelper.WriteLine(_chatProcessor.SentMessageId);
            await _settingsService.UpdateCell("last_tags_message_id", _chatProcessor.SentMessageId);


//            var json = TextHelper.ToJson(tagsData);
            //                Console.WriteLine(json);
        
        }
    }
}