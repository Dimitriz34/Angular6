export interface Application {
  id: number;
  appName: string;
  description?: string;
  appOwner?: string;
  ownerEmail?: string;
  coOwner?: string;
  coOwnerEmail?: string;
  fromEmailAddress?: string;
  fromEmailDisplayName?: string;
  emailServiceName: string;
  emailServiceId: number;
  emailServer?: string;
  port?: number;
  userId: string;
  userName?: string;
  active: number;
  isVerified: boolean;
  isInternalApp: boolean;
  createdDateTime?: string;
  smtpUsername?: string;
  smtpAppPassword?: string;
}

export interface ApplicationCreateDto {
  appName: string;
  description?: string;
  appOwner?: string;
  ownerEmail?: string;
  coOwner?: string;
  coOwnerEmail?: string;
  emailServiceId: number;
  emailServer?: string;
  port?: number;
  userId: string;
  isInternalApp: boolean;
  smtpUsername?: string;
  smtpAppPassword?: string;
}
