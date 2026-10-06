export type PayStructure = 'monthly' | 'per_shift' | 'per_order' | 'annual_lpa';

export type JobType = 'Full Time' | 'Part Time';

export type ExperienceRequirement = 'Any' | 'Fresher Only' | 'Experienced Only';

export type QualificationLevel =
  '< 10th pass' | '10th pass or Above' | '12th pass or Above' | 'Graduate / Post Graduate';

export type GenderPreference = 'Both genders allowed' | 'Male' | 'Female';

export type EnglishLevel =
  | 'Does not speak english'
  | 'Speaks Thoda English'
  | 'Speaks Good English'
  | 'Speaks Fluent English';

export type LocationRange = 'Nearby areas (up to 10 km)' | 'Anywhere in city';

export type CandidateCallCadence =
  'Daily (9 AM - 7 PM)' | 'Weekdays only (Mon - Fri)' | 'Custom hours';

export type HowSoonToFill = 'Immediately (24-48 hrs)' | 'Within 1-2 weeks' | '1 - 3 months';

export type CompanySize =
  | '1 - 10 employees'
  | '11 - 50 employees'
  | '51 - 200 employees'
  | '201 - 1000 employees'
  | '1000+ employees';

export type JobDepartment =
  'All' | 'Logistics' | 'Manufacturing' | 'Customer Support' | 'Operations';

export interface Company {
  id: string;
  name: string;
  logoText: string;
  location: string;
  headquarters: string;
  industry: string;
  verifiedEnterprise: boolean;
  activeDeployments: number;
  workforceStrength: string;
  description: string;
  size?: CompanySize;
}

export interface Job {
  id: string;
  title: string;
  titleHi?: string;
  category: string; // e.g. "Cab Driver", "Delivery Executive", "Telecaller / BPO", "Warehouse Associate", etc.
  jobType: JobType; // 'Full Time' | 'Part Time'
  company: Company;
  employer?: {
    name: string;
    sector?: string;
  };
  location?: string;
  city: string;
  cityHi?: string;
  locality?: string; // e.g. "Andheri East", "Malad", "Chakan MIDC"
  state: string;
  industrialZone?: string;
  workType: 'On-site' | 'Field & Delivery' | 'Hybrid' | 'Remote';
  shiftType: 'Day Shift' | 'Rotational' | 'Night Shift' | 'Flexible / Own Hours';

  // Compensation
  payStructure: PayStructure;
  minPay: number; // e.g. 18000
  maxPay: number; // e.g. 26000
  displayPay: string; // e.g. "₹18,000 - ₹26,000 in-hand"
  monthlyInHandMin: number;
  monthlyInHandMax: number;
  bonusOffer?: string; // e.g. "₹2,500 Attendance Bonus + ₹1,000 Fuel Allowance"
  payoutCadence: 'Daily UPI' | 'Weekly' | 'Monthly (1st)' | 'Bi-weekly';

  // WorkIndia Candidate Requirement Parameters
  experienceRequired: ExperienceRequirement;
  minExperienceYears?: number;
  maxExperienceYears?: number;
  minQualification: QualificationLevel;
  genderPreference: GenderPreference;
  englishRequirement: EnglishLevel;
  candidateLocationRange: LocationRange;
  readyToRelocate: boolean;
  assetsRequired: string[]; // ['Aadhaar Card', 'PAN Card', 'Driving License', '2 Wheeler (Bike)', 'Android Phone', 'Bank Passbook']

  // Operational Details
  candidateCallOption: CandidateCallCadence;
  jobTimings: string; // e.g. "9:30 AM to 6:30 PM (Mon - Sat)"
  address: string;
  howSoonToFill: HowSoonToFill;
  openingsCount: number;
  urgency: 'Immediate Joining (24h)' | 'Hiring Surge' | 'Standard Drive' | 'Campus / Batch';
  postedDate: string;
  complianceBadges: string[];
  verificationRequired: string[];
  languagesPreferred: string[];

  // Additional fields for the job portal component
  department?: JobDepartment;
  tags?: string[];
  salaryMin?: number;
  salaryMax?: number;
  requirements?: string[];
  benefits?: string[];

  // Job Dossier
  summary: string;
  responsibilities: string[];
  qualifications: string[];
  equipmentProvided: string[];
  perks: string[];

  // Recruiter / Enterprise Contact
  enterpriseCoordinator: {
    name: string;
    designation: string;
    phoneHelpline: string;
    whatsappActive: boolean;
    verificationScore: number;
  };

  // Optional legacy collar compatibility
  collar?: string;
  collarLabel?: string;
  collarLabelHi?: string;
}

export interface JobFilterState {
  query: string;
  category: string; // 'all' | 'driver' | 'delivery' | 'telecaller' | 'security' | 'warehouse' | 'cook' | 'sales' | 'back-office'
  jobType: 'all' | 'Full Time' | 'Part Time';
  city: string;
  experienceRequired: 'all' | 'Fresher Only' | 'Experienced Only';
  genderPreference: 'all' | 'Male' | 'Female';
  minMonthlyWage: number;
  immediateJoiningOnly: boolean;
  sortBy: 'urgency' | 'pay' | 'openings' | 'newest';
}

export interface EmployerUser {
  id: string;
  authMethod: 'mobile' | 'email';
  contact: string;
  companyName: string;
  companySize: CompanySize;
  city: string;
  location: string;
  isVerified: boolean;
  postedJobIds: string[];
}

export interface CandidateUser {
  id: string;
  mobile: string;
  name: string;
  city: string;
  locality: string;
  qualification: QualificationLevel;
  experience: ExperienceRequirement;
  englishLevel: EnglishLevel;
  assets: string[];
  isVerified: boolean;
  appliedJobIds: string[];
  savedJobIds: string[];
}

export interface EnterprisePipelineCandidate {
  id: string;
  name: string;
  phone: string;
  roleApplied: string;
  city: string;
  aadhaarVerified: boolean;
  bgvStatus: 'Verified' | 'In Progress' | 'Flagged';
  skillScore: number;
  preferredLanguage: string;
  stage: 'Screening' | 'Document Check' | 'Walk-in Interview' | 'Offer Issued' | 'Deployed';
  appliedDate: string;
  collar?: string;
}

export type PortalJob = Job;
