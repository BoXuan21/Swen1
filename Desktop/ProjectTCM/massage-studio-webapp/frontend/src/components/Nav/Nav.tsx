// Nav.tsx
import { Link } from 'react-router-dom';
import './Nav.css';

export const Nav = () => {
 return (
   <nav className="navbar">
     <div className="nav-container">
       <div className="nav-links">
         <Link to="/" className="logo-link">
           <img src="/images/LogoGold.jpg" alt="Logo" className="logo"/>
         </Link>
         <Link to="/impressum">Impressum</Link>
       </div>
       <div className="admin-link">
         <Link to="/admin/login">Admin</Link>
       </div>
     </div>
   </nav>
 );
};