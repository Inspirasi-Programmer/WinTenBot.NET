﻿using System.Linq;
using Microsoft.AspNetCore.Http;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.Enums;

namespace WinTenBot
{
    public static class When
    {
        public static bool Webhook(IUpdateContext context)  => 
            context.Items.ContainsKey(nameof(HttpContext));

        public static bool NewUpdate(IUpdateContext context) =>
            context.Update != null;
        
        public static bool NewMessage(IUpdateContext context) =>
            context.Update.Message != null;

        public static bool NewTextMessage(IUpdateContext context) =>
            context.Update.Message?.Text != null;

        public static bool NewCommand(IUpdateContext context) =>
            context.Update.Message?.Entities?.FirstOrDefault()?.Type == MessageEntityType.BotCommand;

        public static bool PingReceived(IUpdateContext context) =>
            context.Update.Message.Text.ToLower() == "ping";
        
        public static bool CallTagReceived(IUpdateContext context) =>
            context.Update.Message.Text.Contains('#');

        public static bool MembersChanged(IUpdateContext context) =>
            context.Update.ChannelPost?.NewChatMembers != null ||
            context.Update.ChannelPost?.LeftChatMember != null;

        public static bool LeftChatMember(IUpdateContext context) =>
            context.Update.Message?.LeftChatMember != null;

        public static bool NewChatMembers(IUpdateContext context) =>
            context.Update.Message?.NewChatMembers != null;

        public static bool NewPinnedMessage(IUpdateContext context) =>
            context.Update.Message?.PinnedMessage != null;

        public static bool LocationMessage(IUpdateContext context) =>
            context.Update.Message?.Location != null;

        public static bool StickerMessage(IUpdateContext context) =>
            context.Update.Message?.Sticker != null;

        public static bool MediaReceived(IUpdateContext context) =>
            context.Update.Message?.Document != null ||
            context.Update.Message?.Photo != null;

        public static bool CallbackQuery(IUpdateContext context) =>
            context.Update.CallbackQuery != null;
    }
}