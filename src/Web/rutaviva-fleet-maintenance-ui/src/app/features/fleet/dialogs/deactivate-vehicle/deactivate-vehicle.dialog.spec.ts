import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DeactivateVehicleDialogComponent } from './deactivate-vehicle.dialog';

describe('DeactivateVehicleDialogComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DeactivateVehicleDialogComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('should render impact list for open orders and active routines and emit confirmed', () => {
    const fixture: ComponentFixture<DeactivateVehicleDialogComponent> =
      TestBed.createComponent(DeactivateVehicleDialogComponent);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('plate', 'RVA101');
    fixture.componentRef.setInput('openOrdersCount', 1);
    fixture.componentRef.setInput('activeRoutinesCount', 2);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Desactivar RVA101');
    expect(host.textContent).toContain('1 orden pasa a en pausa');
    expect(host.textContent).toContain('2 rutinas dejan de evaluarse');

    let didConfirm = false;
    fixture.componentInstance.confirmed.subscribe(() => {
      didConfirm = true;
    });

    const confirmBtn: HTMLButtonElement | null = host.querySelector('.btn--danger');
    confirmBtn?.click();
    expect(didConfirm).toBe(true);
  });
});

