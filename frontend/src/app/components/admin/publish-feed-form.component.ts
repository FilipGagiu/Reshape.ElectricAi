import { ChangeDetectionStrategy, Component, OnInit, inject, output, signal } from '@angular/core';
import {
    FormBuilder,
    FormControl,
    FormGroup,
    ReactiveFormsModule,
    Validators,
} from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { MultiSelect } from 'primeng/multiselect';
import { firstValueFrom } from 'rxjs';

import { ALL_ARTIST_NAMES } from '@shared/api/artists';
import { FeedApi, PublishFeedEntryRequest } from '@shared/api/feed-api';
import { Category, CATEGORY_VALUES, FeedEntryDto } from '@shared/api/dto/feed.dto';
import { MUSIC_GENRE_VALUES, MusicGenre } from '@shared/api/enums';
import { extractErrorEnvelope } from '@shared/api/error-envelope';
import { humanizeEnum } from '@shared/api/humanize';

interface PublishFormControls {
    title: FormControl<string>;
    body: FormControl<string>;
    primaryCategory: FormControl<Category>;
    isGeneral: FormControl<boolean>;
    targetArtists: FormControl<string[]>;
    targetGenres: FormControl<MusicGenre[]>;
}

interface SelectOption {
    readonly label: string;
    readonly value: string;
}

type SubmitStatus = 'idle' | 'submitting' | 'success' | 'error';

@Component({
    selector: 'app-publish-feed-form',
    imports: [ReactiveFormsModule, TranslocoModule, MultiSelect],
    templateUrl: './publish-feed-form.component.html',
    styleUrl: './publish-feed-form.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublishFeedFormComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly feedApi = inject(FeedApi);

    readonly published = output<FeedEntryDto>();

    protected readonly categories = CATEGORY_VALUES;
    protected readonly genreOptions: SelectOption[] = MUSIC_GENRE_VALUES.map((value) => ({
        label: humanizeEnum(value),
        value,
    }));
    protected readonly artistOptions: SelectOption[] = ALL_ARTIST_NAMES.map((name) => ({
        label: name,
        value: name,
    }));

    protected readonly status = signal<SubmitStatus>('idle');
    protected readonly errorMessage = signal<string | null>(null);

    protected readonly form: FormGroup<PublishFormControls> = this.fb.nonNullable.group({
        title: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
        body: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(4000)]),
        primaryCategory: this.fb.nonNullable.control<Category>('General', Validators.required),
        isGeneral: this.fb.nonNullable.control(true),
        targetArtists: this.fb.nonNullable.control<string[]>([]),
        targetGenres: this.fb.nonNullable.control<MusicGenre[]>([]),
    });

    ngOnInit(): void {
        // Empty: reactive form already constructed in field initializer.
    }

    protected async submit(): Promise<void> {
        if (this.form.invalid || this.status() === 'submitting') {
            this.form.markAllAsTouched();
            return;
        }

        const raw = this.form.getRawValue();
        const targetArtists = raw.targetArtists;
        const targetGenres = raw.targetGenres;

        if (!raw.isGeneral && targetArtists.length === 0 && targetGenres.length === 0) {
            this.errorMessage.set('admin.publish.error.noTargeting');
            this.status.set('error');
            return;
        }

        const payload: PublishFeedEntryRequest = {
            title: raw.title.trim(),
            body: raw.body.trim(),
            primaryCategory: raw.primaryCategory,
            isGeneral: raw.isGeneral,
            targetArtists,
            targetGenres,
        };

        this.status.set('submitting');
        this.errorMessage.set(null);

        try {
            const result = await firstValueFrom(this.feedApi.publish(payload));
            this.status.set('success');
            this.published.emit(result);
            this.resetForm();
        } catch (err) {
            const envelope = extractErrorEnvelope(err);
            console.warn('[admin] publish failed', envelope);
            this.errorMessage.set(envelope.message || envelope.code);
            this.status.set('error');
        }
    }

    private resetForm(): void {
        this.form.reset({
            title: '',
            body: '',
            primaryCategory: 'General',
            isGeneral: true,
            targetArtists: [],
            targetGenres: [],
        });
    }
}
