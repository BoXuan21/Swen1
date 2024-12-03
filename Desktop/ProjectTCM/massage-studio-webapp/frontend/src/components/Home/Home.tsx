// Home.tsx
import './Home.css';

// Home.tsx
export const Home = () => {
    const services = [
      { title: 'Traditional Massage', description: 'Ancient healing techniques', image: '/images/TCM1.jpg' },
      { title: 'Acupuncture', description: 'Targeted pain relief', image: '/images/TCM2.jpg' },
      { title: 'Cupping', description: 'Deep tissue therapy', image: '/images/TCM3.jpg' },
      { title: 'Chinese Medicine', description: 'Holistic healing', image: '/images/TCM5.jpg' }
    ];

 return (
   <div className="container">
     <div className="grid">
       {services.map(service => (
         <div key={service.title} className="card">
           <img src={service.image} alt={service.title} />
           <div className="content">
             <h2>{service.title}</h2>
             <p>{service.description}</p>
             <button>Details</button>
           </div>
         </div>
       ))}
     </div>
   </div>
 );
};