export interface User { id: number; email: string; fullName: string; roles: string[] }
export interface Session { accessToken: string; expiresAt: string; user: User }
export interface Page<T> { items: T[]; totalCount: number; page: number; pageSize: number; totalPages: number }
export interface Space { id: number; ownerId: number; name: string; description: string; city: string; address: string; capacity: number; hourlyRate: number; isActive: boolean; ownerName: string; workingDays: number; rowVersion: string }
export interface Service { id: number; name: string; pricingType: number; unitPrice: number; isActive: boolean }
export interface Package { id: number; name: string; description: string; price: number; isActive: boolean; items: { spaceServiceId: number; serviceName: string; pricingType: number; quantity: number }[] }
export interface Hours { days: { dayOfWeek: number; opensAt: string; closesAt: string }[] }
export interface QuoteInput { spaceId: number; startsAt: string; endsAt: string; attendeeCount: number; packageId: number | null; additionalServices: { spaceServiceId: number; quantity: number | null }[] }
export interface Quote { spaceSubtotal: number; packagePriceSnapshot: number; servicesSubtotal: number; totalAmount: number; spaceRowVersion: string; services: { nameSnapshot: string; quantity: number; subtotal: number; isIncludedInPackage: boolean }[] }
export interface Booking extends Quote { id: number; spaceId: number; spaceName: string; customerName: string; startsAt: string; endsAt: string; attendeeCount: number; status: number; rowVersion: string; decisionReason: string | null; packageNameSnapshot: string | null }
