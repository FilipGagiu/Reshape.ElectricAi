import { Injectable } from '@angular/core';

import { MOCK_PLAN } from './mock-plan';
import { MOCK_PLAN_UUID, PlanData } from './plan-share.model';

/**
 * Fetches the personalised plan by UUID. Backend not implemented yet:
 * for the demo we return a hardcoded mock for `MOCK_PLAN_UUID` and `null`
 * for everything else (which the viewer renders as a "plan not found"
 * empty state).
 *
 * When the BE lands, replace the body with `firstValueFrom(this.http.get(...))`
 * keeping the same Promise<PlanData | null> signature.
 */
@Injectable({ providedIn: 'root' })
export class PlanShareService {
    private readonly MOCK_LATENCY_MS = 300;

    async getPlanByUuid(uuid: string): Promise<PlanData | null> {
        await new Promise((resolve) => setTimeout(resolve, this.MOCK_LATENCY_MS));
        return uuid === MOCK_PLAN_UUID ? MOCK_PLAN : null;
    }
}
