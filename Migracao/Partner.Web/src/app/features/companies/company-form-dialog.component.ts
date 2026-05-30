import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { CompanyDetail, CompanyUpsertRequest } from './companies.models';

type CompanyFormDialogData = {
  mode: 'create' | 'edit';
  company?: CompanyDetail;
};

@Component({
  selector: 'app-company-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule
  ],
  templateUrl: './company-form-dialog.component.html',
  styleUrl: './company-form-dialog.component.scss'
})
export class CompanyFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CompanyFormDialogComponent, CompanyUpsertRequest | undefined>);
  private readonly dialogData = inject<CompanyFormDialogData>(MAT_DIALOG_DATA);

  readonly isEdit = this.dialogData.mode === 'edit';

  readonly form = this.fb.group({
    cnpj: this.fb.nonNullable.control(this.dialogData.company?.cnpj ?? '', [Validators.required, Validators.maxLength(18)]),
    tradeName: this.fb.nonNullable.control(this.dialogData.company?.tradeName ?? '', [Validators.required, Validators.maxLength(150)]),
    corporateName: this.fb.nonNullable.control(this.dialogData.company?.corporateName ?? '', [Validators.required, Validators.maxLength(150)]),
    managerName: this.fb.nonNullable.control(this.dialogData.company?.managerName ?? '', [Validators.required, Validators.maxLength(120)]),
    isActive: this.fb.nonNullable.control(this.dialogData.company?.isActive ?? true)
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
    const payload: CompanyUpsertRequest = {
      cnpj: raw.cnpj.trim(),
      tradeName: raw.tradeName.trim(),
      corporateName: raw.corporateName.trim(),
      managerName: raw.managerName.trim(),
      isActive: raw.isActive
    };

    this.dialogRef.close(payload);
  }
}

