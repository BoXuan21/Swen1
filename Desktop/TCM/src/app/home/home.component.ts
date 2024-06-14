import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {
  treatments = [
    { title: 'Traditionelle Chinesische Massage', image: 'assets/TCMF/TCM2.jpg', description: 'A relaxing full-body massage to relieve tension and stress.' },
    { title: 'Akupunkturmassage', image: 'assets/TCMF/TCM3.jpg', description: 'Targeted massage to alleviate deep-seated muscle pain and improve mobility.' },
    { title: 'Schröpf und Gua-Sha-Massage', image: 'assets/TCMF/TCM5.jpg', description: 'A therapeutic massage using essential oils to enhance relaxation and healing.' },
    { title: 'Massage bei Regelschmerzen', image: 'assets/TCMF/TCM6.jpg', description: 'A soothing massage with heated stones to melt away muscle tension.' },
    { title: 'Lymphdrainage', image: 'assets/TCMF/TCM7.jpg', description: 'A foot massage that targets specific points to promote overall well-being.' },
  ];
}
