import { computed, Injectable, signal } from '@angular/core';
import {
  RuleGateAuthorizationRequirement,
  RuleGateAuthorizationSnapshot,
  RuleGateAuthorizationStore,
} from '@fotbiler/rulegate-client';

/**
 * Holds the host application's frontend authorization projection.
 *
 * Missing and malformed state denies every check. Matching is exact,
 * ordinal, and case-sensitive.
 */
@Injectable({ providedIn: 'root' })
export class RuleGateAuthorizationClient {
  private readonly store = new RuleGateAuthorizationStore();
  private readonly revision = signal(0);

  readonly isReady = computed(() => {
    this.revision();
    return this.store.isReady;
  });
  readonly snapshot = computed(() => {
    this.revision();
    return this.store.snapshot;
  });

  /**
   * Replaces the complete frontend authorization projection.
   *
   * Returns `false` and clears all grants when any identifier is malformed.
   */
  replaceSnapshot(snapshot: RuleGateAuthorizationSnapshot): boolean {
    const accepted = this.store.replaceSnapshot(snapshot);
    this.notifyChanged();
    return accepted;
  }

  /** Clears the projection and returns the client to fail-closed state. */
  clear(): void {
    this.store.clear();
    this.notifyChanged();
  }

  hasPermission(permission: string): boolean {
    this.revision();
    return this.store.hasPermission(permission);
  }

  hasPolicy(policy: string): boolean {
    this.revision();
    return this.store.hasPolicy(policy);
  }

  hasRole(role: string): boolean {
    this.revision();
    return this.store.hasRole(role);
  }

  isGranted(requirement: RuleGateAuthorizationRequirement | null | undefined): boolean {
    this.revision();
    return this.store.isGranted(requirement);
  }

  private notifyChanged(): void {
    this.revision.update((value) => value + 1);
  }
}
