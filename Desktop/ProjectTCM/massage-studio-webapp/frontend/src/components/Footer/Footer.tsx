import './Footer.css';

export const Footer = () => {
 return (
   <footer className="footer">
     <div className="footer-content">
       <div className="contact">
         <h3>Contact</h3>
         <p>St VeitGasse</p>
         <p>Wien 1130</p>
         <p>Telefon: XXX</p>
       </div>
       <div className="social-links">
         <a href="https://www.facebook.com/chinamassagewien1130" target="_blank" rel="noopener noreferrer">
           <img src="images/facebook.jpg" alt="Facebook" className="social-icon" />
         </a>
       </div>
     </div>
     <div className="copyright">
       © 2024 Chinese Massage Studio
     </div>
   </footer>
 );
};
