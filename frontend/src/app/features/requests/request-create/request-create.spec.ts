import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { RequestCreate } from './request-create';
import { RequestsService } from '../requests.service';
import { CreateRequestInput, RequestVm } from '../requests.models';

function vm(): RequestVm {
  return {
    id: 'new-id',
    title: 'Laptop',
    description: null,
    type: 'Procurement',
    typeLabel: 'Procurement',
    status: 'Draft',
    statusMeta: { label: 'Draft', tone: 'neutral' },
    requesterId: 'r',
    departmentId: 'd',
    createdAt: new Date('2026-05-01T00:00:00Z'),
    approvalSteps: [],
  };
}

// Narrow view over the component's protected members, so the test can drive it without `any`.
interface CreateHarness {
  save(thenSubmit: boolean): void;
  form: { setValue(value: { title: string; type: string; description: string }): void };
}

describe('RequestCreate', () => {
  let created: CreateRequestInput | null;

  function setup() {
    created = null;
    const service = {
      create: (input: CreateRequestInput) => {
        created = input;
        return of(vm());
      },
      submit: () => of(vm()),
    } as unknown as RequestsService;

    TestBed.configureTestingModule({
      imports: [RequestCreate],
      providers: [
        // A matching route so the success-path navigate() resolves instead of rejecting.
        provideRouter([{ path: '**', children: [] }]),
        provideNoopAnimations(),
        { provide: RequestsService, useValue: service },
      ],
    });
    return TestBed.createComponent(RequestCreate);
  }

  it('does not create when the form is invalid', () => {
    const component = setup().componentInstance as unknown as CreateHarness;
    component.save(false);
    expect(created).toBeNull();
  });

  it('creates with the entered values (empty description → null) when valid', () => {
    const component = setup().componentInstance as unknown as CreateHarness;
    component.form.setValue({ title: 'Laptop', type: 'Procurement', description: '' });
    component.save(false);
    expect(created).toEqual({ title: 'Laptop', type: 'Procurement', description: null });
  });
});
