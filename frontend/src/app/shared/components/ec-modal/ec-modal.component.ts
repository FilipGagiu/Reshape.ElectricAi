import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    HostListener,
    Renderer2,
    booleanAttribute,
    effect,
    inject,
    input,
    output,
    viewChild,
} from '@angular/core';

export type EcModalSize = 'sm' | 'md' | 'lg' | 'fullscreen';

const FOCUSABLE_SELECTOR = [
    'a[href]',
    'button:not([disabled])',
    'textarea:not([disabled])',
    'input:not([disabled])',
    'select:not([disabled])',
    '[tabindex]:not([tabindex="-1"])',
].join(',');

/**
 * Reusable Electric Castle modal shell.
 *
 * Visual + behaviour spec: visual-design-language.md §18 Modal & Dialog System.
 * Square corners (--radius-none), --shadow-3 elevation, --opacity-scrim backdrop,
 * slide-up + fade enter, fade-only exit, focus trap, ESC close.
 *
 * Consumers project content via three slots:
 *   <ec-modal [open]="signal" (close)="...">
 *     <header ec-modal-header> ... </header>
 *     <div ec-modal-body> ... </div>
 *     <footer ec-modal-footer> ... </footer>
 *   </ec-modal>
 */
@Component({
    selector: 'ec-modal',
    imports: [],
    templateUrl: './ec-modal.component.html',
    styleUrl: './ec-modal.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.ec-modal-host--open]': 'open()',
    },
})
export class EcModalComponent {
    private readonly hostElement = inject(ElementRef<HTMLElement>);
    private readonly renderer = inject(Renderer2);

    readonly open = input<boolean, boolean>(false, { transform: booleanAttribute });
    readonly size = input<EcModalSize>('fullscreen');
    readonly labelledBy = input<string | undefined>(undefined);
    readonly disableBackdropClose = input<boolean, boolean>(false, {
        transform: booleanAttribute,
    });

    readonly close = output<void>();

    protected readonly containerRef = viewChild<ElementRef<HTMLDivElement>>('container');

    private lastFocusedBeforeOpen: HTMLElement | null = null;

    constructor() {
        effect(() => {
            const opened = this.open();
            if (opened) {
                this.captureExistingFocus();
                this.lockBodyScroll();
                queueMicrotask(() => this.focusFirstElement());
            } else {
                this.unlockBodyScroll();
                this.restoreFocus();
            }
        });
    }

    protected onBackdropMouseDown(event: MouseEvent): void {
        // Close only when the click started AND ended on the backdrop itself,
        // so a click that began inside the dialog and dragged outside doesn't dismiss.
        if (event.target === event.currentTarget && !this.disableBackdropClose()) {
            this.close.emit();
        }
    }

    @HostListener('document:keydown.escape')
    protected onEscape(): void {
        if (this.open()) this.close.emit();
    }

    @HostListener('document:keydown', ['$event'])
    protected onKeydown(event: KeyboardEvent): void {
        if (event.key !== 'Tab') return;
        if (!this.open()) return;

        const container = this.containerRef()?.nativeElement;
        if (!container) return;

        const focusable = Array.from(
            container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR),
        ).filter((el) => el.offsetParent !== null);
        if (focusable.length === 0) return;

        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        const active = document.activeElement as HTMLElement | null;

        if (event.shiftKey && active === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && active === last) {
            event.preventDefault();
            first.focus();
        }
    }

    private captureExistingFocus(): void {
        this.lastFocusedBeforeOpen = (document.activeElement as HTMLElement | null) ?? null;
    }

    private focusFirstElement(): void {
        const container = this.containerRef()?.nativeElement;
        if (!container) return;

        const focusable = container.querySelector<HTMLElement>(FOCUSABLE_SELECTOR);
        focusable?.focus();
    }

    private restoreFocus(): void {
        this.lastFocusedBeforeOpen?.focus();
        this.lastFocusedBeforeOpen = null;
    }

    private lockBodyScroll(): void {
        this.renderer.setStyle(document.body, 'overflow', 'hidden');
    }

    private unlockBodyScroll(): void {
        this.renderer.removeStyle(document.body, 'overflow');
    }
}
