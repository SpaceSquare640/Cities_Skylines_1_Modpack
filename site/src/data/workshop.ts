// Steam Workshop items for each mod. Hidden items are not linked: a visitor who is not the owner
// only sees a Steam error page. Flip `public` once an item is made public on Steam.
export type Edition = 'release' | 'development';
export interface WorkshopItem { id: string; public: boolean; }

export const workshop: Record<string, Record<Edition, WorkshopItem>> = {
	'ai-improve': {
		release: { id: '3782858610', public: true },
		development: { id: '3823575900', public: true },
	},
	transport: {
		release: { id: '3823739383', public: false },
		development: { id: '3823739653', public: true },
	},
	'ai-gaming': {
		release: { id: '3823740106', public: false },
		development: { id: '3823740277', public: true },
	},
};

export const workshopUrl = (id: string) => `https://steamcommunity.com/sharedfiles/filedetails/?id=${id}`;
export const harmonyUrl = workshopUrl('2040656402');
