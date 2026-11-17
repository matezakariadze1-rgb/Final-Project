import { useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { api } from './api';
import type { Page } from './types';

export function Icon({ name, size = 20 }: { name: 'grid' | 'calendar' | 'arrow' | 'pin' | 'people' | 'logout' | 'check' | 'search' | 'inbox'; size?: number }) {
  const paths: Record<string,ReactNode> = {
    grid: <><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></>,
    calendar: <><rect x="3" y="5" width="18" height="16" rx="3"/><path d="M7 3v4m10-4v4M3 11h18m-13 5h3"/></>,
    arrow: <path d="M4 12h16m-6-6 6 6-6 6"/>, pin: <><path d="M20 10c0 6-8 11-8 11S4 16 4 10a8 8 0 1 1 16 0Z"/><circle cx="12" cy="10" r="2.5"/></>,
    people: <><circle cx="9" cy="8" r="3"/><path d="M3 21v-3a6 6 0 0 1 12 0v3m2-16a3 3 0 0 1 0 6m2 10v-3a6 6 0 0 0-2-4"/></>,
    logout: <><path d="M9 4H4v16h5m4-12 4 4-4 4m-5-4h13"/></>, check: <path d="m5 12 4 4L19 6"/>,
    search: <><circle cx="10" cy="10" r="6"/><path d="m15 15 6 6"/></>, inbox: <><path d="M4 4h16l2 13v4H2v-4L4 4Z"/><path d="M2 15h6l2 3h4l2-3h6"/></>
  };
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>;
}
export function ErrorBox({ message }: { message?: string }) { return message ? <div className="error" role="alert">{message}</div> : null; }
export function Loading() { return <div className="state" role="status"><span className="spinner"/>მონაცემები იტვირთება…</div>; }
export function Empty({ title, children }: { title: string; children: ReactNode }) { return <div className="state empty"><Icon name="inbox" size={32}/><h3>{title}</h3><p>{children}</p></div>; }
export function Pager({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (page: number) => void }) {
  if (totalPages <= 1) return null;
  return <nav className="pager" aria-label="გვერდები"><button className="secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>წინა</button><span>{page} / {totalPages}</span><button className="secondary" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>შემდეგი</button></nav>;
}
export function usePage<T>(path: string, token: string, refresh = 0) {
  const [data, setData] = useState<Page<T>>(); const [error, setError] = useState(''); const [loading, setLoading] = useState(true);
  useEffect(() => {
    const controller = new AbortController(); setLoading(true); setError(''); setData(undefined);
    api<Page<T>>(path, token, undefined, controller.signal).then(setData).catch(e => { if (!controller.signal.aborted) setError(e.message); }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [path, token, refresh]);
  return { data, error, loading };
}
export function RoomArt({ variant = 0 }: { variant?: number }) {
  return <div className={`room-art room-${variant % 3}`} aria-hidden="true"><div className="room-window"/><div className="room-picture"/><div className="room-plant"/><div className="room-table"/><span>SPACE / {String(variant + 1).padStart(2, '0')}</span></div>;
}
