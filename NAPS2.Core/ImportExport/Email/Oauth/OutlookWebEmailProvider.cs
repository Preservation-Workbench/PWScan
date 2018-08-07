﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MailKit;
using MimeKit;
using NAPS2.Config;
using Newtonsoft.Json.Linq;

namespace NAPS2.ImportExport.Email.Oauth
{
    public class OutlookWebEmailProvider : IEmailProvider
    {
        private readonly IUserConfigManager userConfigManager;
        private readonly OutlookWebOauthProvider outlookWebOauthProvider;

        public OutlookWebEmailProvider(IUserConfigManager userConfigManager, OutlookWebOauthProvider outlookWebOauthProvider)
        {
            this.userConfigManager = userConfigManager;
            this.outlookWebOauthProvider = outlookWebOauthProvider;
        }

        public bool SendEmail(EmailMessage emailMessage)
        {
            var messageObj = new JObject
            {
                { "Subject", emailMessage.Subject },
                { "Body", new JObject
                {
                    { "ContentType", "Text" },
                    { "Content", emailMessage.BodyText }
                }},
                { "ToRecipients", Recips(emailMessage, EmailRecipientType.To) },
                { "CcRecipients", Recips(emailMessage, EmailRecipientType.Cc) },
                { "BccRecipients", Recips(emailMessage, EmailRecipientType.Bcc) },
                { "Attachments", new JArray(emailMessage.Attachments.Select(attachment => new JObject
                {
                    { "@odata.type", "#Microsoft.OutlookServices.FileAttachment" },
                    { "Name", attachment.AttachmentName },
                    { "ContentBytes", Convert.ToBase64String(File.ReadAllBytes(attachment.FilePath)) }
                }))}
            };
            var respUrl = outlookWebOauthProvider.UploadDraft(messageObj.ToString());

            // Open the draft in the user's browser
            Process.Start(respUrl + "&ispopout=0");

            return true;
        }

        private JToken Recips(EmailMessage message, EmailRecipientType type)
        {
            return new JArray(message.Recipients.Where(recip => recip.Type == type).Select(recip => new JObject
            {
                { "EmailAddress", new JObject
                {
                    { "Address", recip.Address },
                    { "Name", recip.Name }
                }}
            }));
        }
    }
}
