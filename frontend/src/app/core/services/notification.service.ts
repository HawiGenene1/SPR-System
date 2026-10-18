import { Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  constructor(private snackBar: MatSnackBar) {}

  success(message: string, duration: number = 3500): void {
    const config: MatSnackBarConfig = {
      duration,
      panelClass: ['snackbar-success'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    };
    this.snackBar.open(`✓  ${message}`, 'Close', config);
  }

  error(message: string, duration: number = 5000): void {
    const config: MatSnackBarConfig = {
      duration,
      panelClass: ['snackbar-error'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    };
    this.snackBar.open(`✕  ${message}`, 'Dismiss', config);
  }

  warning(message: string, duration: number = 4000): void {
    const config: MatSnackBarConfig = {
      duration,
      panelClass: ['snackbar-warning'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    };
    this.snackBar.open(`⚠  ${message}`, 'OK', config);
  }

  info(message: string, duration: number = 3500): void {
    const config: MatSnackBarConfig = {
      duration,
      panelClass: ['snackbar-info'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    };
    this.snackBar.open(`ℹ  ${message}`, 'OK', config);
  }
}
