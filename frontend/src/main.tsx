import { StrictMode, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import Auth from './Auth';
import Spaces from './Spaces';
import BookSpace from './BookSpace';
import Bookings from './Bookings';
import ManageCatalog from './ManageCatalog';
import { OwnerAccess, OwnerSpaces, NewSpace, ManageSpaceHours } from './OwnerSpaces';
import { api } from './api';
import { Icon } from './shared';
import type { Session, User } from './types';
import './styles.css';
import './desktop-theme.css';

const storageKey = 'spacehub.session';
function restore(): Session | null {
  try {
    const stored = JSON.parse(sessionStorage.getItem(storageKey) || 'null');
    if (stored?.accessToken && typeof stored.user?.fullName === 'string' && Array.isArray(stored.user.roles) && new Date(stored.expiresAt).getTime() > Date.now()) return stored;
  } catch { /* Ignore unavailable storage or an invalid saved session. */ }
  return null;
}
function App() {
  const [session, setSession] = useState<Session | null>(restore); const [notice, setNotice] = useState(''); const [route, setRoute] = useState(location.hash.slice(1) || 'spaces');
  function navigate(path: string) { location.hash = path; }
  function logout(message = '') { setSession(null); setNotice(message); try { sessionStorage.removeItem(storageKey); } catch { /* Memory-only mode still supports logout. */ } navigate('spaces'); }
  function login(value: Session) { try { sessionStorage.setItem(storageKey, JSON.stringify(value)); } catch { /* Continue with an in-memory session. */ } setSession(value); setNotice(''); navigate('spaces'); }
  useEffect(() => { const changed = () => { setRoute(location.hash.slice(1) || 'spaces'); window.scrollTo(0,0); }; window.addEventListener('hashchange', changed); return () => window.removeEventListener('hashchange', changed); }, []);
  useEffect(() => { const expired = () => logout('სესია დასრულდა. გთხოვ, თავიდან შეხვიდე.'); window.addEventListener('session-expired', expired); return () => window.removeEventListener('session-expired', expired); }, []);
  useEffect(() => {
    if (!session) return;
    const controller = new AbortController();
    api<User>('/me', session.accessToken, undefined, controller.signal).catch(() => { /* API dispatches session-expired for invalid credentials; page errors cover network failures. */ });
    const timer = window.setTimeout(() => logout('სესია დასრულდა. გთხოვ, თავიდან შეხვიდე.'), Math.max(0, new Date(session.expiresAt).getTime() - Date.now()));
    return () => { controller.abort(); clearTimeout(timer); };
  }, [session?.accessToken]);
  if (!session) return <Auth onLogin={login} notice={notice}/>;
  const owner = session.user.roles.includes('Owner'); const token = session.accessToken;
  const match = /^space\/(\d+)$/.exec(route);
  const manageMatch = /^manage-space\/(\d+)$/.exec(route);
  const catalogMatch = /^catalog\/(\d+)$/.exec(route);
  const ownerRoute = route === 'my-spaces' || route === 'spaces/new' || !!manageMatch || !!catalogMatch;
  const active = ownerRoute ? 'my-spaces' : match ? 'spaces' : route;
  function updateSession(value: Session) { try { sessionStorage.setItem(storageKey, JSON.stringify(value)); } catch {} setSession(value); }
  const roles: Record<string,string> = { Attendee: 'მონაწილე', Owner: 'მფლობელი', Organizer: 'ორგანიზატორი' };
  return <div className="app-shell"><aside className="sidebar"><a className="brand" href="#spaces"><span className="brand-mark">s</span>spacehub<span className="brand-dot">.</span></a><p className="nav-caption">შენი სივრცე</p><nav><a className={active === 'spaces' ? 'active' : ''} href="#spaces"><Icon name="grid"/>სივრცეები</a><a className={active === 'bookings' ? 'active' : ''} href="#bookings"><Icon name="calendar"/>ჩემი ჯავშნები</a><a className={active === 'my-spaces' ? 'active' : ''} href="#my-spaces"><Icon name="grid"/>ჩემი სივრცეები</a>{owner && <a className={active === 'received' ? 'active' : ''} href="#received"><Icon name="inbox"/>შემოსული ჯავშნები</a>}</nav><div className="sidebar-note"><span className="note-mark">↗</span><strong>იდეას ადგილი სჭირდება.</strong><p>მოძებნე სივრცე, სადაც შენი შემდეგი შეხვედრა შედგება.</p></div><div className="sidebar-bottom"><span className="online-dot"/>ერთად უკეთესი იდეებისთვის</div></aside><div className="workspace"><header className="topbar"><span className="breadcrumb">SpaceHub <span>/</span> {active === 'bookings' ? 'ჩემი ჯავშნები' : active === 'received' ? 'შემოსული ჯავშნები' : active === 'my-spaces' ? 'ჩემი სივრცეები' : 'სივრცეები'}</span><div className="user-menu"><span className="avatar">{session.user.fullName.slice(0,1).toUpperCase()}</span><div><strong>{session.user.fullName}</strong><small>{session.user.roles.map(role => roles[role] || role).join(' · ')}</small></div><button className="logout" onClick={() => logout()} title="გასვლა" aria-label="ანგარიშიდან გასვლა"><Icon name="logout"/></button></div></header><main className="content">{notice && <div className="success" role="status"><Icon name="check"/><span>{notice}</span><button aria-label="შეტყობინების დახურვა" onClick={() => setNotice('')}>×</button></div>}
  {ownerRoute ? (!owner ? <OwnerAccess token={token} onActivate={updateSession}/> : route === 'spaces/new' ? <NewSpace token={token} onCreated={space => { setNotice('სივრცე შექმნილია. დაასრულე დამატება სამუშაო საათების შენახვით.'); navigate(`manage-space/${space.id}`); }}/> : catalogMatch ? <ManageCatalog key={catalogMatch[1]} id={Number(catalogMatch[1])} token={token} userId={session.user.id}/> : manageMatch ? <ManageSpaceHours key={manageMatch[1]} id={Number(manageMatch[1])} token={token} notify={setNotice}/> : <OwnerSpaces token={token}/>) : route === 'spaces' ? <Spaces token={token}/> : match ? <BookSpace key={match[1]} id={Number(match[1])} token={token} onBooked={id => { setNotice(`ჯავშნის მოთხოვნა #${id} გაიგზავნა. დაელოდე მფლობელის დადასტურებას.`); navigate('bookings'); }}/> : route === 'bookings' ? <Bookings token={token} notify={setNotice}/> : route === 'received' && owner ? <Bookings key="received" token={token} received notify={setNotice}/> : <section className="state"><h1>გვერდი ვერ მოიძებნა</h1><a href="#spaces">სივრცეებზე დაბრუნება</a></section>}
  </main><footer className="app-footer"><span>SpaceHub © {new Date().getFullYear()} · ადგილი შენი იდეებისთვის</span><span>დროის სარტყელი: საქართველო (UTC+4)</span></footer></div></div>;
}
createRoot(document.getElementById('root')!).render(<StrictMode><App/></StrictMode>);
