import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { allPages, api, cityName, dayNames, money, tomorrow } from './api';
import type { Booking, Hours, Package, Quote, QuoteInput, Service, Space } from './types';
import { Empty, ErrorBox, Icon, Loading, RoomArt } from './shared';

interface Catalog { space: Space; hours: Hours; services: Service[]; packages: Package[] }
export default function BookSpace({ id, token, onBooked }: { id: number; token: string; onBooked: (id: number) => void }) {
  const [catalog, setCatalog] = useState<Catalog>(); const [loadError, setLoadError] = useState(''); const [refresh, setRefresh] = useState(0);
  const [day, setDay] = useState(tomorrow); const [start, setStart] = useState('10:00'); const [end, setEnd] = useState('13:00'); const [attendees, setAttendees] = useState(10);
  const [packageId, setPackageId] = useState(''); const [extras, setExtras] = useState<Record<number, number>>({});
  const [offer, setOffer] = useState<{ quote: Quote; input: QuoteInput }>(); const [busy, setBusy] = useState(false); const [error, setError] = useState('');
  useEffect(() => {
    const controller = new AbortController(); setCatalog(undefined); setLoadError(''); setOffer(undefined);
    Promise.all([api<Space>(`/spaces/${id}`, token, undefined, controller.signal), api<Hours>(`/spaces/${id}/working-hours`, token, undefined, controller.signal), allPages<Service>(`/spaces/${id}/services`, token, controller.signal), allPages<Package>(`/spaces/${id}/packages`, token, controller.signal)])
      .then(([space, hours, services, packages]) => setCatalog({ space, hours, services: services.filter(s => s.isActive), packages: packages.filter(p => p.isActive) }))
      .catch(e => { if (!controller.signal.aborted) setLoadError(e.message); });
    return () => controller.abort();
  }, [id, token, refresh]);
  function changed() { setOffer(undefined); setError(''); }
  async function calculate(e: FormEvent) {
    e.preventDefault(); if (!catalog) return;
    changed(); setBusy(true);
    try {
      const startsAt = `${day}T${start}:00+04:00`; const endsAt = `${day}T${end}:00+04:00`;
      if (new Date(startsAt).getTime() <= Date.now() || new Date(endsAt).getTime() - new Date(startsAt).getTime() < 3600000) throw new Error('აირჩიე მომავალი დრო და სულ მცირე ერთსაათიანი მონაკვეთი.');
      const input: QuoteInput = { spaceId: id, startsAt, endsAt, attendeeCount: attendees, packageId: packageId ? Number(packageId) : null,
        additionalServices: catalog.services.filter(s => extras[s.id] > 0).map(s => ({ spaceServiceId: s.id, quantity: s.pricingType === 1 ? null : extras[s.id] })) };
      const quote = await api<Quote>('/booking-quotes', token, input); setOffer({ quote, input });
    } catch (e) { setError((e as Error).message); } finally { setBusy(false); }
  }
  async function book() {
    if (!offer || busy) return; setBusy(true); setError('');
    try { const booking = await api<Booking>('/bookings', token, { ...offer.input, spaceRowVersion: offer.quote.spaceRowVersion }); onBooked(booking.id); }
    catch (e) { setOffer(undefined); setError((e as Error).message); } finally { setBusy(false); }
  }
  const selectedPackage = catalog?.packages.find(p => p.id === Number(packageId));
  if (loadError) return <><ErrorBox message={loadError}/><button className="secondary" onClick={() => setRefresh(refresh + 1)}>განახლება</button></>;
  if (!catalog) return <Loading/>;
  const { space, hours, services, packages } = catalog;
  return <><a className="back-link" href="#spaces">← ყველა სივრცე</a><header className="page-title"><div><p className="eyebrow">შენი შეხვედრის ადგილი</p><h1>{space.name}</h1><p className="location"><Icon name="pin" size={16}/>{cityName(space.city)} · {space.address}</p></div><span className="outline-pill"><Icon name="people"/>{space.capacity} სტუმრამდე</span></header>
  <div className="booking-layout"><div><div className="detail-visual"><RoomArt variant={space.id}/><span>სივრცის ილუსტრაცია</span></div><section className="panel about"><h2>სივრცის შესახებ</h2><p>{space.description}</p><div className="facts"><span>მფლობელი<strong>{space.ownerName}</strong></span><span>ღირებულება<strong>{money(space.hourlyRate)} / საათი</strong></span></div><details><summary>სამუშაო საათები · საქართველოს დროით</summary><div className="hours-grid">{[1,2,3,4,5,6,0].map(day => { const hoursForDay = hours.days.find(h => h.dayOfWeek === day); return <div key={day}><span>{dayNames[day]}</span><strong>{hoursForDay ? `${hoursForDay.opensAt.slice(0,5)} – ${hoursForDay.closesAt.slice(0,5)}` : 'დაკეტილია'}</strong></div>; })}</div></details></section>
  {!space.isActive ? <Empty title="სივრცე არააქტიურია">ამ სივრცის დაჯავშნა დროებით შეუძლებელია.</Empty> : <form id="booking-form" onSubmit={calculate}><fieldset disabled={busy}><section className="panel"><div className="step-title"><span>01</span><div><h2>დაგეგმე შეხვედრა</h2><p>დრო მიუთითე საქართველოს დროით (UTC+4).</p></div></div><div className="form-grid"><label>თარიღი<input type="date" value={day} required onChange={e => { setDay(e.target.value); changed(); }}/></label><label>სტუმრები<input type="number" value={attendees} min="1" max={space.capacity} required onChange={e => { setAttendees(Number(e.target.value)); changed(); }}/></label><label>დაწყება<input type="time" step="1800" value={start} required onChange={e => { setStart(e.target.value); changed(); }}/></label><label>დასრულება<input type="time" step="1800" value={end} required onChange={e => { setEnd(e.target.value); changed(); }}/></label></div><p className="hint">მინიმუმ 1 საათი • აირჩიე :00 ან :30 წუთი • ერთი დღის ფარგლებში</p></section>
  <section className="panel"><div className="step-title"><span>02</span><div><h2>შეავსე შენი გამოცდილება</h2><p>აირჩიე პაკეტი და საჭირო მომსახურებები.</p></div></div><label>პაკეტი<select value={packageId} onChange={e => { setPackageId(e.target.value); changed(); }}><option value="">პაკეტის გარეშე</option>{packages.map(p => <option key={p.id} value={p.id}>{p.name} — {money(p.price)}</option>)}</select></label>
  {selectedPackage && <div className="package-info"><strong>{selectedPackage.name}</strong><p>{selectedPackage.description}</p><ul>{selectedPackage.items.map(item => <li key={item.spaceServiceId}>{item.serviceName} · {item.quantity} {item.pricingType === 1 ? 'სტუმარზე' : 'ერთეული'}</li>)}</ul><p className="hint">პაკეტზე მეტი სტუმრის მომსახურება ავტომატურად დაემატება ფასს.</p></div>}
  <h3 className="extras-heading">დამატებითი მომსახურებები</h3>{!services.length && <p className="muted">ამ სივრცეს დამატებითი მომსახურებები ჯერ არ აქვს.</p>}
  {services.map(service => { const included = selectedPackage?.items.find(i => i.spaceServiceId === service.id); const autoIncluded = !!included && service.pricingType === 1; return <div className="service-row" key={service.id}><label className="check-label"><input type="checkbox" checked={autoIncluded || !!extras[service.id]} disabled={autoIncluded || busy} onChange={e => { setExtras({ ...extras, [service.id]: e.target.checked ? 1 : 0 }); changed(); }}/><span><strong>{service.name}</strong><small>{money(service.unitPrice)} / {service.pricingType === 1 ? 'სტუმარი' : 'ერთეული'}{included ? ' · პაკეტშიც შედის' : ''}</small></span></label>{service.pricingType === 0 && !!extras[service.id] && <input aria-label={`${service.name} — დამატებითი რაოდენობა`} className="quantity" type="number" min="1" max="10000" required value={extras[service.id]} onChange={e => { setExtras({ ...extras, [service.id]: Number(e.target.value) }); changed(); }}/>}</div>; })}
  <p className="hint">ერთეულებით დათვლილი მომსახურება პაკეტში შეტანილ რაოდენობას ემატება. სტუმარზე დათვლილი რაოდენობა ავტომატურია.</p></section></fieldset></form>}</div>
  <aside className="quote-panel"><div className="panel"><span className="eyebrow">შენი ჯავშანი</span><h2>ყველაფერი ერთ ადგილას</h2><p className="muted">გადაამოწმე ფასი და გაუგზავნე მოთხოვნა მფლობელს.</p><div className="price-start"><strong>{money(space.hourlyRate)}</strong><span> / საათი</span></div><ErrorBox message={error}/>
  {offer ? <div className="quote-breakdown" aria-live="polite"><div><span>სივრცე</span><strong>{money(offer.quote.spaceSubtotal)}</strong></div><div><span>პაკეტი</span><strong>{money(offer.quote.packagePriceSnapshot)}</strong></div><div><span>დამატებითი მომსახურება</span><strong>{money(offer.quote.servicesSubtotal)}</strong></div>{offer.quote.services.map((line,i) => <p className="quote-line" key={i}>{line.nameSnapshot} × {line.quantity} <span>{line.isIncludedInPackage ? 'პაკეტშია' : money(line.subtotal)}</span></p>)}<div className="quote-total"><span>ჯამი</span><strong>{money(offer.quote.totalAmount)}</strong></div><button className="primary full" disabled={busy} onClick={book}>{busy ? 'იგზავნება…' : 'ჯავშნის მოთხოვნა'}<Icon name="arrow"/></button></div> : <><div className="quote-placeholder"><Icon name="calendar" size={28}/><p>შეავსე დეტალები და მიიღე ზუსტი ფასი.</p></div><button className="primary full" type="submit" form="booking-form" disabled={busy || !space.isActive}>{busy ? 'მოწმდება…' : 'ფასისა და დროის შემოწმება'}</button></>}
  <p className="hint quote-hint"><Icon name="check" size={16}/>ჯავშანი ძალაში შევა მფლობელის დადასტურების შემდეგ.</p><p className="hint">ონლაინ გადახდა არ ხდება. ფასის შემოწმება დროს არ იკავებს.</p></div><div className="quote-tip"><strong>ჯერ სივრცე, შემდეგ ღონისძიება</strong><p>დადასტურებულ ჯავშანზე ორგანიზატორს ღონისძიების მოწყობა შეუძლია.</p></div></aside></div></>;
}
