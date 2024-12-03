import { useState } from 'react';
import './Login.css';
import showPasswordIcon from '/images/ShowPassword.jpg'; // Das Bild importieren

export const Login = () => {
  const [credentials, setCredentials] = useState({ username: '', password: '' });
  const [showPassword, setShowPassword] = useState(false); // Zustand für das Anzeigen des Passworts

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    console.log('Login attempt:', credentials);
  };

  const togglePasswordVisibility = () => {
    setShowPassword(prevState => !prevState); // Umschalten der Sichtbarkeit
  };

  return (
    <div className="login-container">
      <form className="login-form" onSubmit={handleSubmit}>
        <h2>Admin Login</h2>
        <div className="form-group">
          <input
            type="text"
            placeholder="Username"
            value={credentials.username}
            onChange={e => setCredentials({ ...credentials, username: e.target.value })}
          />
        </div>
        <div className="form-group input-container">
          <input
            type={showPassword ? 'text' : 'password'} // Passworttyp umschalten
            placeholder="Password"
            value={credentials.password}
            onChange={e => setCredentials({ ...credentials, password: e.target.value })}
          />
          <img
            src={showPasswordIcon}
            alt="Show Password"
            className="show-password"
            onClick={togglePasswordVisibility} // Klick zum Umschalten
          />
        </div>
        <button type="submit">Login</button>
      </form>
    </div>
  );
};
