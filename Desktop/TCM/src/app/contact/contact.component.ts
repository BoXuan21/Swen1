import { Component } from '@angular/core';

@Component({
  selector: 'app-contact',
  templateUrl: './contact.component.html',
  styleUrls: ['./contact.component.css']
})
export class ContactComponent {
  contactMethods = [
    { title: 'Telefonnummer', name: 'China Massage Team', icon: 'assets/TCMF/TCM7.jpg', description: '+43(0)677 61386068' },
    { title: 'Addresse', icon: 'assets/TCMF/Maps.jpg', description: 'St. Veit Gasse 74. 1130 Wien Austria' }
  ];
}
