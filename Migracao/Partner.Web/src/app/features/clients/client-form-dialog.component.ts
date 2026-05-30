import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { ClientCompanyLookupItem, ClientDetail, ClientUpsertRequest } from './clients.models';

type ClientFormDialogData = {
  mode: 'create' | 'edit';
  client?: ClientDetail;
  companies: ClientCompanyLookupItem[];
};

@Component({
  selector: 'app-client-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule
  ],
  templateUrl: './client-form-dialog.component.html',
  styleUrl: './client-form-dialog.component.scss'
})
export class ClientFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<ClientFormDialogComponent, ClientUpsertRequest | undefined>);
  private readonly dialogData = inject<ClientFormDialogData>(MAT_DIALOG_DATA);

  readonly isEdit = this.dialogData.mode === 'edit';
  readonly companies = this.dialogData.companies;

  readonly form = this.fb.group({
    firstName: this.fb.nonNullable.control(this.dialogData.client?.firstName ?? '', [Validators.required, Validators.maxLength(120)]),
    lastName: this.fb.nonNullable.control(this.dialogData.client?.lastName ?? '', [Validators.required, Validators.maxLength(120)]),
    document: this.fb.nonNullable.control(this.dialogData.client?.document ?? '', [Validators.required, Validators.maxLength(20)]),
    email: this.fb.nonNullable.control(this.dialogData.client?.email ?? '', [Validators.required, Validators.email, Validators.maxLength(150)]),
    gender: this.fb.nonNullable.control(this.dialogData.client?.gender ?? '', [Validators.maxLength(40)]),
    birthDate: this.fb.nonNullable.control(this.dialogData.client?.birthDate ?? '', [Validators.maxLength(20)]),
    companyId: this.fb.nonNullable.control(this.dialogData.client?.companyId ?? 0, [Validators.required, Validators.min(1)]),
    department: this.fb.nonNullable.control(this.dialogData.client?.department ?? '', [Validators.maxLength(120)]),
    role: this.fb.nonNullable.control(this.dialogData.client?.role ?? '', [Validators.maxLength(120)]),
    approved: this.fb.nonNullable.control(this.dialogData.client?.approved ?? false),
    isActive: this.fb.nonNullable.control(this.dialogData.client?.isActive ?? true)
  });

  close(): void {
    this.dialogRef.close(undefined);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload: ClientUpsertRequest = {
      firstName: raw.firstName.trim(),
      lastName: raw.lastName.trim(),
      document: raw.document.trim(),
      email: raw.email.trim(),
      gender: raw.gender.trim() || undefined,
      birthDate: raw.birthDate.trim() || undefined,
      companyId: raw.companyId,
      department: raw.department.trim() || undefined,
      role: raw.role.trim() || undefined,
      approved: raw.approved,
      isActive: raw.isActive
    };

    this.dialogRef.close(payload);
  }
}

