import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { ImpressumComponent } from './components/impressum/impressum.component';
import { LoginComponent } from './components/admin/login/login.component';
import { DashboardComponent } from './components/admin/dashboard/dashboard.component';
import { ReceiptFormComponent } from './components/admin/receipt-form/receipt-form.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'impressum', component: ImpressumComponent },
  { path: 'admin/login', component: LoginComponent },
  { path: 'admin/dashboard', component: DashboardComponent },
  { path: 'admin/receipt', component: ReceiptFormComponent }
];
