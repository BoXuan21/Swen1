import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Nav } from './components/Nav/Nav';
import { Home } from './components/Home/Home';
import { Impressum } from './components/Impressum/Impressum';
import { Login } from './components/Admin/Login/Login';
import { Footer } from './components/Footer/Footer';

function App() {
  return (
    <BrowserRouter>
      <Nav />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/impressum" element={<Impressum />} />
        <Route path="/admin/login" element={<Login />} />
      </Routes>
      <Footer />
    </BrowserRouter>
    
  );
}

export default App;