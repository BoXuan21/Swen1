// Impressum.tsx
import './Impressum.css';

export const Impressum = () => {
 return (
   <div className="impressum-container">
     <h1>Impressum</h1>
     
     <section>
       <h2>Angaben gemäß § 5 TMG:</h2>
       <p>Vorname Nachname</p>
       <p>Straße Nr</p>
       <p>PLZ Stadt</p>
     </section>

     <section>
       <h2>Kontakt:</h2>
       <p>Telefon: XXXX/XXXXXX</p>
       <p>E-Mail: name@domain.de</p>
     </section>

     <section>
       <h2>Umsatzsteuer-ID:</h2>
       <p>Umsatzsteuer-Identifikationsnummer gemäß §27 a Umsatzsteuergesetz:</p>
       <p>DE XXX XXX XXX</p>
     </section>
   </div>
 );
};