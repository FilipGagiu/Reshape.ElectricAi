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

        // Track the visual viewport so the modal "squishes" when the mobile
        // soft keyboard opens, instead of being pushed off-screen.
        //
        // Two metrics matter, not just height:
        //  - visualViewport.height  → keyboard shrinks the visible area
        //  - visualViewport.offsetTop → iOS auto-scrolls the focused input
        //    into view, which shifts the visual viewport DOWN inside the
        //    layout viewport. The backdrop must follow that offset, or it
        //    ends up positioned above the visible area.
        //
        // `window.visualViewport` is the standardised API (iOS 13+,
        // Chrome 61+, Firefox 91+). `100dvh` alone is not reliable across
        // iOS versions.
        effect((onCleanup) => {
            if (!this.open()) return;

            const vv = window.visualViewport;
            if (!vv) {
                this.applyVisualMetrics(0, window.innerHeight);
                return;
            }

            const handler = () => this.applyVisualMetrics(vv.offsetTop, vv.height);
            handler();
            vv.addEventListener('resize', handler);
            vv.addEventListener('scroll', handler);

            onCleanup(() => {
                vv.removeEventListener('resize', handler);
                vv.removeEventListener('scroll', handler);
            });
        });
    }

    private applyVisualMetrics(top: number, height: number): void {
        const el = this.hostElement.nativeElement;
        el.style.setProperty('--ec-modal-vt', `${top}px`);
        el.style.setProperty('--ec-modal-vh', `${height}px`);
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
