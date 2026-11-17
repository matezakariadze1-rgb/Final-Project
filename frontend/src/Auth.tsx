import { useState } from 'react';
import type { FormEvent } from 'react';
import { api } from './api';
import type { Session } from './types';
import { ErrorBox, Icon } from './shared';

export default function Auth({ onLogin, notice }: { onLogin: (session: Session) => void; notice: string }) {
  const [register, setRegister] = useState(false); const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [name, setName] = useState(''); const [busy, setBusy] = useState(false); const [error, setError] = useState('');
  async function submit(e: FormEvent) {
    e.preventDefault(); setError(''); setBusy(true);
    try { onLogin(await api<Session>(register ? '/auth/register' : '/auth/login', undefined, { email: email.trim(), password, ...(register ? { fullName: name.trim() } : {}) })); }
    catch (e) { setError((e as Error).message); } finally { setBusy(false); }
  }
  return <main className="auth-shell"><section className="auth-story"><a className="brand" href="#"><span className="brand-mark">s</span>spacehub<span className="brand-dot">.</span></a><div className="story-copy"><p className="eyebrow">კარგი შეხვედრა აქ იწყება</p><h1>შენი იდეა.<br/>შენი სივრცე.</h1><p>იპოვე ადგილი შეხვედრისთვის, აირჩიე საჭირო მომსახურებები და დაგეგმე ყველაფერი ერთ სივრცეში.</p><div className="story-line"><span>01 / აირჩიე</span><span>02 / დაჯავშნე</span><span>03 / შეიკრიბეთ</span></div></div><div className="auth-architecture" aria-hidden="true"><i/><i/><i/><b/></div><small>სივრცეები • შეხვედრები • შესაძლებლობები</small></section>
  <section className="auth-form-wrap"><div className="auth-form"><p className="eyebrow">კეთილი იყოს შენი დაბრუნება</p><h2>{register ? 'შექმენი ანგარიში' : 'შედი შენს სივრცეში'}</h2><p className="muted">{register ? 'მოემზადე შენი შემდეგი შეხვედრისთვის.' : 'შენი შემდეგი შეხვედრა ერთი ნაბიჯით ახლოსაა.'}</p>
  {notice && <div className="notice" role="status">{notice}</div>}<ErrorBox message={error}/>
  <form onSubmit={submit}><fieldset disabled={busy}>{register && <label>სახელი და გვარი<input autoComplete="name" value={name} onChange={e => setName(e.target.value)} required minLength={2} maxLength={120}/></label>}
  <label>ელფოსტა<input type="email" autoComplete="username" placeholder="name@example.com" value={email} onChange={e => setEmail(e.target.value)} required maxLength={254}/></label>
  <label>პაროლი<input type="password" autoComplete={register ? 'new-password' : 'current-password'} value={password} onChange={e => setPassword(e.target.value)} required minLength={register ? 8 : 1} maxLength={128}/></label>
  <button className="primary full" type="submit">{busy ? 'გთხოვ, დაელოდე…' : register ? 'ანგარიშის შექმნა' : 'შესვლა'}<Icon name="arrow"/></button></fieldset></form>
  <p className="auth-switch">{register ? 'უკვე გაქვს ანგარიში?' : 'ჯერ არ გაქვს ანგარიში?'} <button className="text-button" disabled={busy} onClick={() => { setRegister(!register); setError(''); }}>{register ? 'შესვლა' : 'რეგისტრაცია'}</button></p><div className="auth-note"><Icon name="check"/><span>აირჩიე სივრცე და მიიღე ფასის შეთავაზება დაჯავშნამდე.</span></div>
  </div><span className="auth-footer">SpaceHub — ადგილი შენი იდეებისთვის</span></section></main>;
}
