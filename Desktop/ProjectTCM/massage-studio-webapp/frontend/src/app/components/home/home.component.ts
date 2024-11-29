// home.component.ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
 selector: 'app-home',
 standalone: true,
 imports: [CommonModule],
 templateUrl: './home.component.html',
 styleUrls: ['./home.component.css']
})
export class HomeComponent {
 services = [
   { title: 'Traditional Massage', description: 'Ancient healing techniques', image: 'https://via.placeholder.com/400x300' },
   { title: 'Acupuncture', description: 'Targeted pain relief', image: 'https://via.placeholder.com/400x300' },
   { title: 'Cupping', description: 'Deep tissue therapy', image: 'https://via.placeholder.com/400x300' }
 ];
}