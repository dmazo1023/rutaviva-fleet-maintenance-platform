import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SERVICE_ORDERS_TEXTS } from '../../service-orders.texts';
import { ServiceOrdersComingSoonPageComponent } from './coming-soon.page';

describe('ServiceOrdersComingSoonPageComponent', () => {
  it('should show the next phase message without any HTTP dependency', async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceOrdersComingSoonPageComponent],
      providers: [provideZonelessChangeDetection(), provideRouter([])],
    }).compileComponents();
    const fixture: ComponentFixture<ServiceOrdersComingSoonPageComponent> = TestBed.createComponent(
      ServiceOrdersComingSoonPageComponent
    );
    fixture.detectChanges();
    const text: string = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain(SERVICE_ORDERS_TEXTS.comingSoonTitle);
    expect(text).toContain(SERVICE_ORDERS_TEXTS.comingSoonMessage);
  });
});
