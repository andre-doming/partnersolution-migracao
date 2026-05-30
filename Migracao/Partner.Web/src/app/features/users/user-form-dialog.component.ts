import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { UserCompanyLookupItem, UserDetail, UserFunctionLookupItem, UserUpsertRequest } from './users.models';

type UserFormDialogData = {
  mode: 'create' | 'edit';
  user?: UserDetail;
  companies: UserCompanyLookupItem[];
  functions: UserFunctionLookupItem[];
};

@Component({
  selector: 'app-user-form-dialog',
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
  templateUrl: './user-form-dialog.component.html',
  styleUrl: './user-form-dialog.component.scss'
})
export class UserFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<UserFormDialogComponent, UserUpsertRequest | undefined>);
  private readonly dialogData = inject<UserFormDialogData>(MAT_DIALOG_DATA);

  readonly isEdit = this.dialogData.mode === 'edit';
  readonly companies = this.dialogData.companies;
  readonly functions = this.dialogData.functions;

  readonly form = this.fb.group({
    name: this.fb.nonNullable.control(this.dialogData.user?.name ?? '', [Validators.required, Validators.maxLength(120)]),
    login: this.fb.nonNullable.control(this.dialogData.user?.login ?? '', [Validators.required, Validators.maxLength(60)]),
    password: this.fb.nonNullable.control('', [Validators.minLength(8), Validators.maxLength(120)]),
    email: this.fb.nonNullable.control(this.dialogData.user?.email ?? '', [Validators.required, Validators.email, Validators.maxLength(150)]),
    isAdmin: this.fb.nonNullable.control(this.dialogData.user?.isAdmin ?? false),
    accessToken: this.fb.nonNullable.control(this.dialogData.user?.accessToken ?? false),
    isActive: this.fb.nonNullable.control(this.dialogData.user?.isActive ?? true),
    companyIds: this.fb.nonNullable.control<number[]>(this.dialogData.user?.companyIds ?? [], [Validators.required]),
    functionIds: this.fb.nonNullable.control<number[]>(this.dialogData.user?.functionIds ?? [], [Validators.required])
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

    const payload: UserUpsertRequest = {
      name: raw.name.trim(),
      login: raw.login.trim(),
      password: raw.password.trim() || undefined,
      email: raw.email.trim(),
      isAdmin: raw.isAdmin,
      accessToken: raw.accessToken,
      isActive: raw.isActive,
      companyIds: raw.companyIds,
      functionIds: raw.functionIds
    };

    if (this.isEdit && !payload.password) {
      delete payload.password;
    }

    this.dialogRef.close(payload);
  }
}

