export interface Environment {
    readonly production: boolean;
    readonly apiBaseUrl: string;
    readonly allowDevBypass: boolean;
}
