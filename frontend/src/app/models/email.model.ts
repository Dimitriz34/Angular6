export interface Email {
  emailId: string;
  appName: string;
  userId: string;
  username: string;
  upn: string;
  sender: string;
  fromEmailAddress: string;
  subject: string;
  body: string;
  serviceName: string;
  status: string;
  errorCode?: string;
  errorMessage?: string;
  creationDateTime: string;
  active: number;
}

export interface EmailPost {
  subject: string;
  body?: string;
  isHtml: boolean;
  toRecipients: string;
  ccRecipients?: string;
  appId?: string | number | null;
  appPassword?: string;
  smtpUserEmail?: string;
  useTPAssist?: boolean;  // Optional flag, defaults to false
}

export interface TPAssistRequest {
  body: string;
  subject?: string;
  isHtml: boolean;
}

export interface TPAssistResult {
  success: boolean;
  body: string;
  errorMessage?: string;
  isHtml: boolean;
}

export interface EmailRecipient {
  id: string;
  emailId: string;
  toDisplayName?: string;
  recipient: string;
  recipientType: string;
  createDateTime: string;
}

export interface EmailAttachment {
  emailId: string;
  attachmentName: string;
  attachmentPath?: string;
  attachmentType?: string;
}

export interface EmailDetail {
  emailId: string;
  userId?: string;
  appCode?: number;
  appName?: string;
  senderFrom?: string;
  replyTo?: string;
  recipients?: string;
  ccRecipients?: string;
  bccRecipients?: string;
  subject?: string;
  body?: string;
  isHtmlBody: boolean;
  priority?: string;
  status?: string;
  errorCode?: string;
  errorMessage?: string;
  retryCount: number;
  maxRetry: number;
  scheduledDateTime?: string;
  sentDateTime?: string;
  trackingId?: string;
  graphMessageId?: string;
  active: boolean;
  createdDateTime?: string;
  createdBy?: string;
  modifiedDateTime?: string;
  modifiedBy?: string;
  serviceName?: string;
  upn?: string;
  username?: string;
  emailRecipients: EmailRecipient[];
  emailAttachments: EmailAttachment[];
}
