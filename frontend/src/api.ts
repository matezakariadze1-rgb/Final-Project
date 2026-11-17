import type { Page } from './types';

const translations: Record<string, string> = {
  'Deactivate packages using this service before deactivating it.': 'ამ მომსახურებას აქტიური პაკეტი იყენებს. ჯერ შეცვალე ან გათიშე შესაბამისი პაკეტი.',
  'Pricing type cannot change while the service belongs to a package.': 'პაკეტში გამოყენებული მომსახურების დათვლის წესი ვერ შეიცვლება. ჯერ ამოიღე მომსახურება პაკეტებიდან.',
  'All services must belong to this space; active packages require active services.': 'აირჩიე ამ სივრცის მომსახურებები. აქტიურ პაკეტში მხოლოდ აქტიური მომსახურებები შეიძლება შედიოდეს.',

  'Space is not active.': 'სივრცე ამჟამად არააქტიურია.',
  'Booking must be in the future.': 'აირჩიე მომავალი თარიღი და დასრულება დაწყების შემდეგ.',
  'Attendees exceed the space capacity.': 'სტუმრების რაოდენობა სივრცის ტევადობას აღემატება.',
  "Booking must fit within one local day's working hours.": 'ჯავშანი უნდა მოთავსდეს ერთი დღის სამუშაო საათებში.',
  'Space is already booked during this period.': 'ამ მონაკვეთში სივრცე უკვე დაჯავშნილია. აირჩიე სხვა დრო.',
  'The record changed. Reload it and retry.': 'მონაცემები შეიცვალა. განაახლე გვერდი და თავიდან გამოთვალე ფასი.',
  'Pending bookings can be cancelled before start; confirmed bookings require at least 24 hours notice.': 'დადასტურებული ჯავშანი უქმდება დაწყებამდე სულ მცირე 24 საათით ადრე.',
  'This booking can no longer be cancelled.': 'ამ ჯავშნის გაუქმება აღარ არის შესაძლებელი.',
  'Owner cancellation requires a reason.': 'გაუქმების მიზეზის მითითება აუცილებელია.',
  'Only pending bookings can be confirmed.': 'დადასტურება მხოლოდ მოლოდინში მყოფ ჯავშანს შეუძლია.',
  'Only pending bookings can be rejected.': 'უარყოფა მხოლოდ მოლოდინში მყოფ ჯავშანს შეუძლია.',
  'Selected service is inactive or does not belong to this space.': 'არჩეული მომსახურება აღარ არის ხელმისაწვდომი. განაახლე გვერდი.',
  'Selected package is inactive or does not belong to this space.': 'არჩეული პაკეტი აღარ არის ხელმისაწვდომი. განაახლე გვერდი.'
};
const fields: Record<string,string> = { name: 'დასახელება', price: 'პაკეტის ფასი', items: 'პაკეტის შემადგენლობა', unitPrice: 'ერთეულის ფასი', pricingType: 'დათვლის წესი', isActive: 'აქტიურობა', description: 'აღწერა', address: 'მისამართი', city: 'ქალაქი', capacity: 'ტევადობა', hourlyRate: 'საათის ფასი', days: 'სამუშაო დღეები', email: 'ელფოსტა', password: 'პაროლი', fullName: 'სახელი და გვარი', startsAt: 'დაწყების დრო', endsAt: 'დასრულების დრო', attendeeCount: 'სტუმრების რაოდენობა', additionalServices: 'დამატებითი მომსახურებები', reason: 'მიზეზი', rowVersion: 'ჩანაწერის ვერსია', spaceRowVersion: 'სივრცის ვერსია' };
export async function api<T>(path: string, token?: string, body?: unknown, signal?: AbortSignal, method?: 'GET' | 'POST' | 'PUT' | 'DELETE'): Promise<T> {
  let response: Response;
  try {
    response = await fetch('/api' + path, {
      method: method ?? (body === undefined ? 'GET' : 'POST'), signal,
      headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(body === undefined ? {} : { 'Content-Type': 'application/json' }) },
      ...(body === undefined ? {} : { body: JSON.stringify(body) })
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error;
    throw new Error('სერვერთან კავშირი ვერ დამყარდა. თუ მოთხოვნას აგზავნიდი, გამეორებამდე შეამოწმე „ჩემი ჯავშნები“.');
  }
  if (!response.ok) {
    if (response.status === 401 && token) window.dispatchEvent(new Event('session-expired'));
    const detail = await response.json().catch(() => ({}));
    const invalidFields = Object.keys(detail.errors || {}).map(key => { const field = key.split(/[.\[]/)[0]; return fields[field.charAt(0).toLowerCase() + field.slice(1)]; }).filter(Boolean);
    const fallback: Record<number,string> = {
      400: 'შეამოწმე შევსებული მონაცემები და მითითებული დროის სისწორე.',
      401: token ? 'სესია დასრულდა. გთხოვ, თავიდან შეხვიდე.' : 'ელფოსტა ან პაროლი არასწორია.',
      403: 'ამ მოქმედების შესრულების უფლება არ გაქვს.',
      404: 'მოთხოვნილი ჩანაწერი ვერ მოიძებნა.',
      409: path.startsWith('/auth/register') ? 'ეს ელფოსტა უკვე გამოყენებულია.' : 'მონაცემები შეიცვალა ან არჩეული დრო დაკავებულია. განაახლე გვერდი.'
    };
    throw new Error(translations[detail.message] || (invalidFields.length ? `გადაამოწმე: ${invalidFields.join(', ')}.` : fallback[response.status]) || 'მოთხოვნა ვერ შესრულდა. სცადე მოგვიანებით.');
  }
  return response.status === 204 ? undefined as T : response.json();
}
export async function allPages<T>(path: string, token: string, signal?: AbortSignal) {
  const first = await api<Page<T>>(`${path}?pageSize=100&page=1`, token, undefined, signal);
  const result = [...first.items];
  for (let page = 2; page <= first.totalPages; page++) result.push(...(await api<Page<T>>(`${path}?pageSize=100&page=${page}`, token, undefined, signal)).items);
  return result;
}
export const money = (value: number) => new Intl.NumberFormat('ka-GE', { maximumFractionDigits: 2 }).format(value) + ' ₾';
export function dateTime(value: string) {
  const parts = new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Tbilisi', day: 'numeric', month: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(new Date(value));
  const part = (type: string) => parts.find(p => p.type === type)?.value || '';
  const months = ['იან', 'თებ', 'მარ', 'აპრ', 'მაი', 'ივნ', 'ივლ', 'აგვ', 'სექ', 'ოქტ', 'ნოე', 'დეკ'];
  return `${part('day')} ${months[Number(part('month')) - 1]} ${part('year')}, ${part('hour')}:${part('minute')}`;
}
export const statusNames = ['მოლოდინში', 'დადასტურებული', 'უარყოფილი', 'გაუქმებული', 'დასრულებული'];
export const dayNames = ['კვირა', 'ორშაბათი', 'სამშაბათი', 'ოთხშაბათი', 'ხუთშაბათი', 'პარასკევი', 'შაბათი'];
export const cityName = (city: string) => ({ Tbilisi: 'თბილისი', Batumi: 'ბათუმი', Kutaisi: 'ქუთაისი' }[city] || city);
export function tomorrow() { return new Date(Date.now() + 28 * 3600000).toISOString().slice(0, 10); }
