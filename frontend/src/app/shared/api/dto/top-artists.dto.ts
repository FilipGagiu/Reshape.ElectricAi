export interface TopArtistsResponse {
    readonly artists: ReadonlyArray<string>;
}

export interface TopArtistRow {
    readonly rank: number;
    readonly name: string;
    readonly imagePath: string;
}
