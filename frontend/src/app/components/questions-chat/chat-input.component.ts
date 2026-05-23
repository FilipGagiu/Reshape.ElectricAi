import {
    AfterViewInit,
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    booleanAttribute,
    computed,
    effect,
    inject,
    input,
    output,
    signal,
    viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

/**
 * EC chat input. Two visual variants:
 *  - 'sticky' (default): dark-navy bar with 3px EC Red top stripe, off-white
 *    inset textarea, yellow send with hover glow. Renders 3 suggestion chips
 *    above the input on first visit, dismissed on first interaction.
 *  - 'inline': lighter chrome for use inside a modal footer (no chips,
 *    no dark bar, no entrance animation).
 *
 * Visual spec: visual-design-language.md §14 Form UI + this turn's plan
 * (engaged ux-designer + ui-designer for the dark-bar / chips treatment).
 */
@Component({
    selector: 'app-chat-input',
    imports: [ReactiveFormsModule, TranslocoModule],
    templateUrl: './chat-input.component.html',
    styleUrl: './chat-input.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.ec-chat-input-host--inline]': "variant() === 'inline'",
        '[class.ec-chat-input-host--sticky]': "variant() === 'sticky'",
    },
})
export class ChatInputComponent implements AfterViewInit {
    private readonly transloco = inject(TranslocoService);

    readonly variant = input<'sticky' | 'inline'>('sticky');
    readonly placeholderKey = input<string>('questions.input.placeholder');
    readonly autoFocus = input<boolean, boolean>(false, { transform: booleanAttribute });
    readonly disabled = input<boolean, boolean>(false, { transform: booleanAttribute });

    readonly send = output<string>();

    protected readonly textControl = new FormControl<string>('', { nonNullable: true });
    protected readonly textareaRef = viewChild<ElementRef<HTMLTextAreaElement>>('textarea');

    private readonly textValue = toSignal(this.textControl.valueChanges, { initialValue: '' });
    private readonly hasUserInteracted = signal(false);

    /** Static list of suggestion i18n keys; only used in the sticky variant. */
    protected readonly suggestionKeys: ReadonlyArray<string> = [
        'questions.input.suggestion.parking',
        'questions.input.suggestion.camping',
        'questions.input.suggestion.travel',
    ];

    protected readonly canSend = computed(
        () => this.textValue().trim().length > 0 && !this.disabled(),
    );

    /** Show chips only on sticky variant, before any interaction and while empty. */
    protected readonly showSuggestions = computed(() => {
        if (this.variant() !== 'sticky') return false;
        if (this.hasUserInteracted()) return false;
        return this.textValue().length === 0;
    });

    constructor() {
        effect(() => {
            const isDisabled = this.disabled();
            if (isDisabled) this.textControl.disable({ emitEvent: false });
            else this.textControl.enable({ emitEvent: false });
        });

        effect(() => {
            // Re-measure textarea height whenever the value changes.
            this.textValue();
            queueMicrotask(() => this.resizeTextarea());
        });
    }

    ngAfterViewInit(): void {
        if (this.autoFocus()) {
            queueMicrotask(() => this.textareaRef()?.nativeElement.focus());
        }
        this.resizeTextarea();
    }

    protected onKeydown(event: KeyboardEvent): void {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            this.submit();
        }
    }

    protected onTextInput(): void {
        // Any keystroke is an interaction; chips disappear once the user starts typing.
        if (!this.hasUserInteracted()) this.hasUserInteracted.set(true);
    }

    protected applySuggestion(key: string): void {
        const text = this.transloco.translate(key);
        this.textControl.setValue(text);
        queueMicrotask(() => {
            this.textareaRef()?.nativeElement.focus();
            this.resizeTextarea();
        });
    }

    protected submit(): void {
        if (!this.canSend()) return;
        const text = this.textControl.value.trim();
        if (!text) return;

        this.hasUserInteracted.set(true);
        this.send.emit(text);
        this.textControl.setValue('');
        queueMicrotask(() => this.resizeTextarea());
    }

    private resizeTextarea(): void {
        const textarea = this.textareaRef()?.nativeElement;
        if (!textarea) return;

        textarea.style.height = 'auto';
        const lineHeight = 22; // matches CSS line-height for 14px * 1.55
        const maxLines = 5;
        const maxHeight = lineHeight * maxLines;
        const next = Math.min(textarea.scrollHeight, maxHeight);
        textarea.style.height = `${next}px`;
        textarea.style.overflowY = textarea.scrollHeight > maxHeight ? 'auto' : 'hidden';
    }

    protected sendAriaLabel(): string {
        return this.transloco.translate('questions.input.sendAria');
    }
}
