import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ENTERPRISE_COMPANIES, INDIA_WAGE_BENCHMARKS, MOCK_ENTERPRISE_PIPELINE } from './job-data';
import { JobCardComponent } from './job-card.component';
import {
  CandidateUser,
  CompanySize,
  EnglishLevel,
  EnterprisePipelineCandidate,
  ExperienceRequirement,
  GenderPreference,
  HowSoonToFill,
  Job,
  JobFilterState,
  JobType,
  QualificationLevel,
} from './job.model';
import { JobService } from './job.service';

type PortalTab = 'portal' | 'enterprise' | 'wages' | 'companies';
type ModalKind = 'candidate' | 'employer' | 'post-job' | 'application' | 'saved' | null;

interface PostJobDraft {
  title: string;
  category: string;
  jobType: JobType;
  companyName: string;
  companySize: CompanySize;
  city: string;
  locality: string;
  address: string;
  openingsCount: number;
  howSoonToFill: HowSoonToFill;
  experienceRequired: ExperienceRequirement;
  minQualification: QualificationLevel;
  genderPreference: GenderPreference;
  englishRequirement: EnglishLevel;
  monthlyInHandMin: number;
  monthlyInHandMax: number;
  bonusOffer: string;
  jobTimings: string;
  description: string;
}

@Component({
  selector: 'app-job-portal',
  imports: [FormsModule, JobCardComponent],
  templateUrl: './job-portal.component.html',
  styleUrl: './job-portal.component.scss',
})
export class JobPortalComponent {
  private readonly jobService = inject(JobService);

  readonly companies = ENTERPRISE_COMPANIES;
  readonly wageBenchmarks = INDIA_WAGE_BENCHMARKS;
  readonly roleCategories = [
    'all',
    'Cab & Van Drivers',
    'Delivery Executives',
    'Telecalling & BPO',
    'Warehouse & Logistics',
    'Security & Facility',
    'Cooks & Hospitality',
    'Retail & Field Sales',
    'Back Office & Data Entry',
  ];
  readonly cityOptions = [
    'All India Cities',
    'Mumbai',
    'Delhi-NCR',
    'Bengaluru',
    'Pune',
    'Hyderabad',
    'Chennai',
    'Ahmedabad',
    'Kolkata',
  ];

  readonly activeTab = signal<PortalTab>('portal');
  readonly activeModal = signal<ModalKind>(null);
  readonly selectedJob = signal<Job | null>(null);
  readonly applyingJob = signal<Job | null>(null);
  readonly savedJobIds = signal<string[]>(['in-job-01', 'in-job-02']);
  readonly toastMessage = signal<string | null>(null);

  readonly filters = signal<JobFilterState>({
    query: '',
    category: 'all',
    jobType: 'all',
    city: '',
    experienceRequired: 'all',
    genderPreference: 'all',
    minMonthlyWage: 0,
    immediateJoiningOnly: false,
    sortBy: 'urgency',
  });

  readonly candidateUser = signal<CandidateUser | null>({
    id: 'cand-demo',
    mobile: '9876543210',
    name: 'Rahul Sharma',
    city: 'Mumbai',
    locality: 'Malad West',
    qualification: '10th pass or Above',
    experience: 'Fresher Only',
    englishLevel: 'Speaks Thoda English',
    assets: ['Aadhaar Card', 'PAN Card', 'Driving License', '2 Wheeler (Bike)', 'Android Phone'],
    isVerified: true,
    appliedJobIds: ['in-job-01'],
    savedJobIds: ['in-job-01', 'in-job-02'],
  });

  readonly employerUser = signal({
    id: 'emp-demo',
    authMethod: 'mobile' as const,
    contact: '9820144520',
    companyName: 'Ola Smart Fleet Services',
    companySize: '1000+ employees' as CompanySize,
    city: 'Mumbai',
    location: 'Andheri East',
    isVerified: true,
    postedJobIds: ['in-job-01'],
  });

  readonly candidateProfile = signal({
    mobile: '',
    name: '',
    city: 'Mumbai',
    locality: 'Malad West',
    qualification: '10th pass or Above' as QualificationLevel,
    experience: 'Fresher Only' as ExperienceRequirement,
    englishLevel: 'Speaks Thoda English' as EnglishLevel,
  });

  readonly employerProfile = signal({
    contact: '',
    companyName: '',
    companySize: '11 - 50 employees' as CompanySize,
    city: 'Mumbai',
    location: 'Andheri East',
  });

  readonly applicationDraft = signal({
    fullName: '',
    phone: '',
    preferredLanguage: 'Hindi',
    experienceLevel: '1-2 Years',
    aadhaarVerified: true,
    whatsappUpdates: true,
    token: '',
  });

  readonly postJobStep = signal<1 | 2 | 3>(1);
  readonly postJobDraft = signal<PostJobDraft>({
    title: 'Cab Driver',
    category: 'Cab & Van Drivers',
    jobType: 'Full Time',
    companyName: 'Tata EV Logistics',
    companySize: '201 - 1000 employees',
    city: 'Mumbai',
    locality: 'Malad West',
    address: 'Unit 402, Building 7, Mindspace IT Park, Malad West',
    openingsCount: 15,
    howSoonToFill: 'Immediately (24-48 hrs)',
    experienceRequired: 'Any',
    minQualification: '< 10th pass',
    genderPreference: 'Male',
    englishRequirement: 'Does not speak english',
    monthlyInHandMin: 22000,
    monthlyInHandMax: 32000,
    bonusOffer: 'Rs 2,500 monthly punctuality bonus + Rs 1,000 festive incentive',
    jobTimings: '9:30 AM to 6:30 PM (6 days/week)',
    description:
      'Drive commercial passenger cab or EV fleet across city routes. Safe driving, smartphone navigation, and customer politeness required.',
  });

  readonly pipelineCandidates = signal<EnterprisePipelineCandidate[]>([
    ...MOCK_ENTERPRISE_PIPELINE,
  ]);
  readonly activeStageFilter = signal<string>('all');
  readonly pipelineQuery = signal('');

  readonly filteredJobs = computed(() => {
    const filters = this.filters();

    return this.jobService
      .jobs()
      .filter((job) => {
        const query = filters.query.trim().toLowerCase();
        const text = [
          job.title,
          job.company.name,
          job.city,
          job.locality,
          job.category,
          job.summary,
        ]
          .join(' ')
          .toLowerCase();

        return (
          (filters.category === 'all' || job.category === filters.category) &&
          (filters.jobType === 'all' || job.jobType === filters.jobType) &&
          (filters.experienceRequired === 'all' ||
            job.experienceRequired === filters.experienceRequired ||
            job.experienceRequired === 'Any') &&
          (filters.genderPreference === 'all' ||
            job.genderPreference === filters.genderPreference ||
            job.genderPreference === 'Both genders allowed') &&
          (!filters.city ||
            job.city.toLowerCase().includes(filters.city.toLowerCase()) ||
            job.locality?.toLowerCase().includes(filters.city.toLowerCase())) &&
          (!filters.immediateJoiningOnly ||
            job.urgency === 'Immediate Joining (24h)' ||
            job.howSoonToFill.includes('Immediately')) &&
          (!filters.minMonthlyWage || job.monthlyInHandMax >= filters.minMonthlyWage) &&
          (!query || text.includes(query))
        );
      })
      .sort((a, b) => {
        if (filters.sortBy === 'pay') {
          return b.monthlyInHandMax - a.monthlyInHandMax;
        }
        if (filters.sortBy === 'openings') {
          return b.openingsCount - a.openingsCount;
        }
        if (filters.sortBy === 'urgency') {
          const score = (job: Job) =>
            job.urgency.includes('Immediate') ? 3 : job.urgency.includes('Surge') ? 2 : 1;
          return score(b) - score(a);
        }
        return 0;
      });
  });

  readonly savedJobs = computed(() =>
    this.jobService.jobs().filter((job) => this.savedJobIds().includes(job.id)),
  );

  readonly pipelineStages = computed(() => {
    const candidates = this.pipelineCandidates();
    return [
      'all',
      'Screening',
      'Document Check',
      'Walk-in Interview',
      'Offer Issued',
      'Deployed',
    ].map((stage) => ({
      id: stage,
      label: stage === 'all' ? 'All Candidates' : stage,
      count:
        stage === 'all'
          ? candidates.length
          : candidates.filter((candidate) => candidate.stage === stage).length,
    }));
  });

  readonly filteredCandidates = computed(() => {
    const stage = this.activeStageFilter();
    const query = this.pipelineQuery().trim().toLowerCase();
    return this.pipelineCandidates().filter((candidate) => {
      const text = [
        candidate.name,
        candidate.roleApplied,
        candidate.city,
        candidate.preferredLanguage,
      ]
        .join(' ')
        .toLowerCase();
      return (stage === 'all' || candidate.stage === stage) && (!query || text.includes(query));
    });
  });

  readonly reachScore = computed(() => {
    const draft = this.postJobDraft();
    let score = 48;
    if (draft.monthlyInHandMax >= 25000) score += 18;
    if (draft.monthlyInHandMax >= 30000) score += 10;
    if (draft.experienceRequired !== 'Experienced Only') score += 12;
    if (draft.genderPreference === 'Both genders allowed') score += 8;
    if (draft.englishRequirement !== 'Speaks Fluent English') score += 5;
    return Math.min(score, 98);
  });

  setFilter<K extends keyof JobFilterState>(key: K, value: JobFilterState[K]): void {
    this.filters.update((current) => ({ ...current, [key]: value }));
  }

  setPostDraft<K extends keyof PostJobDraft>(key: K, value: PostJobDraft[K]): void {
    this.postJobDraft.update((current) => ({ ...current, [key]: value }));
  }

  isJobSaved(jobId: string): boolean {
    return this.savedJobIds().includes(jobId);
  }

  toggleSavedJob(jobId: string): void {
    this.savedJobIds.update((ids) => {
      const exists = ids.includes(jobId);
      this.showToast(exists ? 'Removed from saved roles.' : 'Role saved for later review.');
      return exists ? ids.filter((id) => id !== jobId) : [...ids, jobId];
    });
  }

  openJob(job: Job): void {
    this.selectedJob.set(job);
  }

  startApplication(job: Job): void {
    this.applyingJob.set(job);
    this.selectedJob.set(null);
    this.applicationDraft.update((draft) => ({
      ...draft,
      token: '',
      fullName: draft.fullName,
      phone: draft.phone,
    }));
    this.activeModal.set('application');
  }

  submitApplication(): void {
    const token = `IW-PASS-${Math.floor(100000 + Math.random() * 900000)}`;
    this.applicationDraft.update((draft) => ({ ...draft, token }));
    this.showToast('Interview pass generated locally for prototype review.');
  }

  saveCandidateProfile(): void {
    const profile = this.candidateProfile();
    this.candidateUser.set({
      id: `cand-${Date.now()}`,
      mobile: profile.mobile || '9876543210',
      name: profile.name || 'Candidate User',
      city: profile.city,
      locality: profile.locality,
      qualification: profile.qualification,
      experience: profile.experience,
      englishLevel: profile.englishLevel,
      assets: ['Aadhaar Card', 'PAN Card', 'Android Phone'],
      isVerified: true,
      appliedJobIds: [],
      savedJobIds: [],
    });
    this.activeModal.set(null);
    this.showToast('Candidate profile saved locally.');
  }

  saveEmployerProfile(): void {
    const profile = this.employerProfile();
    this.employerUser.set({
      id: `emp-${Date.now()}`,
      authMethod: 'mobile',
      contact: profile.contact || '9820144520',
      companyName: profile.companyName || 'Direct Employer',
      companySize: profile.companySize,
      city: profile.city,
      location: profile.location,
      isVerified: true,
      postedJobIds: [],
    });
    this.activeModal.set(null);
    this.showToast('Employer workspace activated locally.');
  }

  publishJob(): void {
    const draft = this.postJobDraft();
    const job: Job = {
      id: `in-custom-${Date.now()}`,
      title: draft.title,
      titleHi: `${draft.title} भर्ती`,
      category: draft.category,
      jobType: draft.jobType,
      company: {
        id: `company-${Date.now()}`,
        name: draft.companyName,
        logoText: draft.companyName.slice(0, 3).toUpperCase(),
        location: `${draft.locality}, ${draft.city}`,
        headquarters: draft.city,
        industry: `${draft.category} Staffing`,
        verifiedEnterprise: true,
        activeDeployments: draft.openingsCount,
        workforceStrength: draft.companySize,
        size: draft.companySize,
        description: `Direct employer vacancy for ${draft.title}.`,
      },
      city: draft.city,
      cityHi: draft.city,
      locality: draft.locality,
      state: 'India',
      workType: 'On-site',
      shiftType: 'Day Shift',
      payStructure: 'monthly',
      minPay: draft.monthlyInHandMin,
      maxPay: draft.monthlyInHandMax,
      displayPay: `Rs ${draft.monthlyInHandMin.toLocaleString('en-IN')} - Rs ${draft.monthlyInHandMax.toLocaleString('en-IN')} / mo`,
      monthlyInHandMin: draft.monthlyInHandMin,
      monthlyInHandMax: draft.monthlyInHandMax,
      bonusOffer: draft.bonusOffer,
      payoutCadence: 'Monthly (1st)',
      experienceRequired: draft.experienceRequired,
      minQualification: draft.minQualification,
      genderPreference: draft.genderPreference,
      englishRequirement: draft.englishRequirement,
      candidateLocationRange: 'Nearby areas (up to 10 km)',
      readyToRelocate: true,
      assetsRequired: ['Aadhaar Card', 'PAN Card', 'Android Phone'],
      candidateCallOption: 'Daily (9 AM - 7 PM)',
      jobTimings: draft.jobTimings,
      address: draft.address,
      howSoonToFill: draft.howSoonToFill,
      openingsCount: draft.openingsCount,
      urgency: draft.howSoonToFill.includes('Immediately')
        ? 'Immediate Joining (24h)'
        : 'Hiring Surge',
      postedDate: 'Just now',
      complianceBadges: ['Direct employer', 'Zero agency fee', 'Aadhaar-ready intake'],
      verificationRequired: ['Aadhaar Card', 'Active Phone'],
      languagesPreferred: ['Hindi', 'English'],
      summary: draft.description,
      responsibilities: [
        'Report on time for assigned duty.',
        'Follow site safety and attendance rules.',
        'Keep required documents available during joining.',
      ],
      qualifications: [draft.minQualification],
      equipmentProvided: ['Company ID Badge'],
      perks: ['Direct company payroll', draft.bonusOffer, 'Interview scheduling support'],
      enterpriseCoordinator: {
        name: 'Direct HR Helpline',
        designation: 'Recruitment Manager',
        phoneHelpline: '+91 80698 24660',
        whatsappActive: true,
        verificationScore: 98,
      },
    };
    this.jobService.addJob(job);
    this.activeModal.set(null);
    this.postJobStep.set(1);
    this.showToast('Job published locally and added to the active role index.');
  }

  selectCompany(companyName: string): void {
    this.setFilter('query', companyName);
    this.activeTab.set('portal');
  }

  advanceCandidate(candidateId: string): void {
    const order: EnterprisePipelineCandidate['stage'][] = [
      'Screening',
      'Document Check',
      'Walk-in Interview',
      'Offer Issued',
      'Deployed',
    ];
    this.pipelineCandidates.update((candidates) =>
      candidates.map((candidate) => {
        if (candidate.id !== candidateId) return candidate;
        const next = order[Math.min(order.indexOf(candidate.stage) + 1, order.length - 1)];
        return { ...candidate, stage: next };
      }),
    );
    this.showToast('Pipeline stage updated in the local ATS board.');
  }

  resetFilters(): void {
    this.filters.set({
      query: '',
      category: 'all',
      jobType: 'all',
      city: '',
      experienceRequired: 'all',
      genderPreference: 'all',
      minMonthlyWage: 0,
      immediateJoiningOnly: false,
      sortBy: 'urgency',
    });
  }

  private showToast(message: string): void {
    this.toastMessage.set(message);
    window.setTimeout(() => this.toastMessage.set(null), 3200);
  }
}
