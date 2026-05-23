export enum FeedItemKind {
    NowPlaying = 'nowPlaying',
    UpNext = 'upNext',
    Alert = 'alert',
    ScheduleChange = 'scheduleChange',
    Photo = 'photo',
    Info = 'info',
    Social = 'social',
}

export enum AlertLevel {
    Info = 'info',
    Warning = 'warning',
    Danger = 'danger',
}

interface FeedItemBase {
    id: string;
    publishedAt: Date;
}

export interface NowPlayingItem extends FeedItemBase {
    kind: FeedItemKind.NowPlaying;
    artist: string;
    stage: string;
    startsAt: Date;
    endsAt: Date;
}

export interface UpNextItem extends FeedItemBase {
    kind: FeedItemKind.UpNext;
    artist: string;
    stage: string;
    startsAt: Date;
}

export interface AlertItem extends FeedItemBase {
    kind: FeedItemKind.Alert;
    level: AlertLevel;
    title: string;
    body: string;
    icon: string;
}

export interface ScheduleChangeItem extends FeedItemBase {
    kind: FeedItemKind.ScheduleChange;
    artist: string;
    newStage: string;
    newTime: Date;
    reason?: string;
}

export interface PhotoItem extends FeedItemBase {
    kind: FeedItemKind.Photo;
    author: string;
    caption: string;
    accent: string;
}

export interface InfoItem extends FeedItemBase {
    kind: FeedItemKind.Info;
    title: string;
    body: string;
    icon: string;
}

export interface SocialItem extends FeedItemBase {
    kind: FeedItemKind.Social;
    primaryUser: string;
    othersCount: number;
    location: string;
}

export type FeedItem =
    | NowPlayingItem
    | UpNextItem
    | AlertItem
    | ScheduleChangeItem
    | PhotoItem
    | InfoItem
    | SocialItem;
